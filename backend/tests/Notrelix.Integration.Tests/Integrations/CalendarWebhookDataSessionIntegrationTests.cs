using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Integrations.Webhooks;
using Notrelix.Infrastructure.Options;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integrations;

/// <summary>
/// TAC-AI-FLOW-07 — the frozen Option-A mechanism proof: the webhook command
/// is write-classified, so a delivery travels ISender → the canonical request
/// pipeline (RequestContract → ExecutionContext → DataSession) → the real
/// EfRequestDataSession transaction → the real verifier → the real intake →
/// PostgreSQL. Case 1 commits the Processed receipt; case 2 pins a semantic
/// that is not obvious: a business/security rejection is a Result.Failure,
/// not an exception — the transaction still commits exactly one Rejected
/// diagnostic receipt.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarWebhookDataSessionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private const string Provider = "google";
    private const string ProviderSecret = "calendar-google-webhook-secret";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CalendarWebhookDataSessionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task VerifiedCallback_ThroughCanonicalPipeline_CommitsProcessedReceipt()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new HandleCalendarWebhookCommand(Provider, signature, timestamp, body));

        result.Succeeded.Should().BeTrue("the data session commits the accepted claim");

        // The command has returned; the data-session transaction is committed —
        // read the durable receipt from a fresh context.
        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
        receipt.Status.Should().Be("Processed",
            "the canonical write pipeline committed the claim inside the data-session transaction");
        receipt.PayloadHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RejectedCallback_IsBusinessFailure_NotTransactionRollback()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, _, timestamp) = SignedCallback(externalEventId, Now);
        var forgedSignature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes("wrong-secret"))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        // A forged callback fails verification as a RESULT, not an exception —
        // the DataSession has no exception to roll back on.
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new HandleCalendarWebhookCommand(Provider, forgedSignature, timestamp, body));

        result.Succeeded.Should().BeFalse("a forged signature is a security rejection");

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId.StartsWith("rejected:"));
        receipt.Status.Should().Be("Rejected",
            "the rejected diagnostic receipt still commits — business rejection != transaction rollback");
        receipt.FailureReason.Should().NotBeNullOrWhiteSpace();
        receipt.ProcessedAt.Should().BeNull();
    }

    private static (string Body, string Signature, string Timestamp) SignedCallback(
        string externalEventId, DateTimeOffset occurredAt)
    {
        var body = JsonSerializer.Serialize(new { eventId = externalEventId, kind = "calendar#event" });
        var timestamp = occurredAt.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return (body, signature, timestamp);
    }

    /// <summary>
    /// The canonical pipeline composition around the REAL verifier and the
    /// REAL intake — no webhook seam is mocked. Only transport-adjacent
    /// identity plumbing is faked (the intake is anonymous and global).
    /// </summary>
    private ServiceProvider CreateProvider()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);

        // The intake is anonymous and global — no credential context exists;
        // the pipeline still requires the ambient service to be present.
        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.None);

        // Payload protection uses the real purpose-scoped envelope contract;
        // the key material itself is exercised by the Security suite.
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((plain, purpose) => $"protected:{purpose}:{plain}");
        encryptor.Setup(e => e.Unprotect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((cipher, _) => cipher[(cipher.IndexOf(':') + 1)..][(cipher.IndexOf(':') + 1)..]);

        var webhookOptions = Options.Create(new CalendarWebhookOptions
        {
            Providers = new Dictionary<string, CalendarWebhookOptions.CalendarWebhookProviderOptions>
            {
                [Provider] = new() { Enabled = true, SharedSecret = ProviderSecret },
            },
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(HandleCalendarWebhookCommand).Assembly));
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddSingleton<IDateTimeProvider>(clockMock.Object);
        services.AddSingleton<ISecretEncryptor>(encryptor.Object);
        services.AddSingleton<ICurrentCredentialContext>(credentialMock.Object);
        services.AddSingleton(webhookOptions);

        services.AddScoped<ICalendarWebhookVerifier, CalendarWebhookVerifier>();
        services.AddScoped<ICalendarWebhookIntake, CalendarWebhookIntake>();
        services.AddScoped<CalendarWebhookVerifier>();
        services.AddScoped<CalendarWebhookIntake>();
        services.AddScoped<
            IRequestHandler<HandleCalendarWebhookCommand, Result>,
            HandleCalendarWebhookCommandHandler>();

        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddScoped<ApplicationDbContext>(_ => _db.CreateContext(tenant));
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Abstractions.IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Documents.Abstractions.IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Collaboration.Abstractions.ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Governance.Abstractions.IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Automation.Abstractions.IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Workspaces.Abstractions.IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Features.Accounts.Abstractions.IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // The canonical write pipeline: the request contract classifies the
        // command as transactional, and EfRequestDataSession owns the real
        // PostgreSQL transaction the receipt claim runs inside.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddSingleton<PipelineMetrics>();
        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();
        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        return services.BuildServiceProvider();
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
