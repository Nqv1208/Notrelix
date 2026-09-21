using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Application.Features.Integrations.Calendar.Processing;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Integrations.Webhooks;
using Notrelix.Infrastructure.Options;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC v2.6 WAVE-E — the Wave-E split production graph: the bootstrap commits a
/// NON-TERMINAL "Captured" claim plus exactly one provider-neutral
/// <c>calendar_webhook_processing_requested</c> outbox row in the same real
/// transaction, and the real delivery chain (outbox dispatcher → MassTransit
/// InMemory receive pipeline → TenantContextConsumeFilter → real
/// <see cref="CalendarWebhookProcessingRequestedConsumer"/>) restores the
/// workspace tenant before running the consumer, then durably records
/// "Blocked" (the explicit SemanticTargetUndefined terminal) — never a false
/// "Processed" and never a poison retry. A duplicate delivery converges to the
/// same single receipt and single enrollment.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarWebhookProcessingRuntimeTests : IAsyncLifetime
{
    private const string Provider = "google";
    private const string ProviderSecret = "calendar-google-webhook-secret";
    private const string WebhookPath = "rt-test-webhook-path-abc123";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CalendarWebhookProcessingRuntimeTests(PostgresTestContainer db)
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
    public async Task VerifiedCallback_EnrollsExactlyOneOutboxRow_AndConsumerBlocksUnderDerivedTenant()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId);

        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        Guid receiptId;
        Guid outboxId;

        // 1. Bootstrap through the canonical request pipeline with the real
        //    webhook vertical. The claim + the outbox row commit atomically in
        //    the data-session transaction.
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath));

            result.Succeeded.Should().BeTrue("the data session commits the accepted claim");
        }

        // 2. Durable bootstrap state: one Captured (non-terminal) claim and
        //    exactly one outbox row with the authoritative tenant envelope.
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var outbox = await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .SingleAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId);
            outboxId = outbox.Id;
            outbox.SchemaVersion.Should().Be(1);
            outbox.AccountId.Should().Be(accountId,
                "the outbox row must carry the authoritative tenant envelope");
            outbox.WorkspaceId.Should().Be(workspaceId);
            receiptId = outbox.PayloadJson.RootElement.GetProperty("receiptId").GetGuid();

            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == receiptId);
            receipt.Status.Should().Be("Captured",
                "the bootstrap decides a non-terminal claim only; the consumer decides the terminal state");
            receipt.ProcessedAt.Should().BeNull();
            receipt.ConnectionId.Should().Be(connectionId);
        }

        // 3. Start the real delivery chain and let the consumer decide.
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        recorder.Reset();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var dispatched = await WaitForOutboxProcessedAsync(outboxId);
            dispatched.Should().BeTrue("the dispatcher must deliver the committed processing intent");

            var blocked = await WaitForReceiptStatusAsync(receiptId, "Blocked");
            blocked.Should().BeTrue(
                "the real consumer must record the durable BLOCKED-DECISION terminal state");

            (await OutboxRowCountAsync(accountId, workspaceId)).Should().Be(1,
                "exactly one processing enrollment per callback");

            await using var final = _db.CreateContext(SystemTenant());
            var terminal = await final.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == receiptId);
            terminal.Status.Should().Be("Blocked",
                "Wave E: SemanticTargetUndefined is a durable explicit non-success terminal, never a false Processed");
            terminal.ProcessedAt.Should().BeNull("a Blocked receipt is not a success");
            terminal.FailureReason.Should().Contain("semantic target undefined");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before the consumer runs");
        recorder.LastWorkspaceAccountId.Should().Be(accountId);
        recorder.LastWorkspaceId.Should().Be(workspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse(
            "a workspace-scoped processing intent must not run its consumer as System");
        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    [Fact]
    public async Task DuplicateDelivery_ConvergesToSingleReceipt_AndSingleConsumerDecision()
    {
        var (_, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId);

        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        Guid receiptId;

        // The same callback is delivered twice (each delivery is its own request
        // scope, as in HTTP): the payload-hash/connection claim must converge
        // both into a single non-terminal receipt and a single outbox enrollment.
        var command = new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath);
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            (await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            (await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue("a duplicate delivery is an idempotent acceptance, not an error");
        }

        {
            await using var probe = _db.CreateContext(SystemTenant());
            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
            receipt.Status.Should().Be("Captured",
                "two deliveries produce exactly one non-terminal claim");
            receiptId = receipt.Id;

            (await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId)).Should().Be(1,
                "two deliveries produce exactly one outbox enrollment");
        }

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        recorder.Reset();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var blocked = await WaitForReceiptStatusAsync(receiptId, "Blocked");
            blocked.Should().BeTrue("the consumer must reach the single durable terminal decision");

            await using var final = _db.CreateContext(SystemTenant());
            (await final.InboundWebhookReceipts.IgnoreQueryFilters()
                .CountAsync(r => r.ExternalEventId == externalEventId)).Should().Be(1,
                "no duplicate receipt is created by a redelivered callback");
            (await final.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId)).Should().Be(1,
                "no duplicate enrollment is created by a redelivered callback");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "the single consumer run must still execute under the restored workspace tenant");
        recorder.LastWorkspaceAccountId.Should().Be(accountId);
        recorder.LastWorkspaceId.Should().Be(workspaceId);
    }

    // ── composition -----------------------------------------------------------

    private ServiceProvider BuildProvider(TenantObservationRecorder recorder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["Messaging:Transport"] = "InMemory",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
            })
            .Build();

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
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddOptions();
        services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        services.AddSingleton(environment.Object);

        services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser());
        services.AddScoped<ICurrentTenantContext>(_ =>
            new RecordingCurrentTenantContext(new CurrentTenantContext(), recorder));

        // The granular production graph.
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddObservability(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddCrossContextBindings();

        // Application-provided runtime seams this graph must bind explicitly:
        // the domain-event outbox collector and the Integrations processing seam.
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<ICalendarWebhookProcessingUseCase, CalendarWebhookProcessingUseCase>();

        // The canonical request pipeline around the REAL webhook vertical.
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(HandleCalendarWebhookCommand).Assembly));
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddSingleton<ISecretEncryptor>(encryptor.Object);
        services.AddSingleton(webhookOptions);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.None);
        services.AddSingleton<ICurrentCredentialContext>(credentialMock.Object);

        services.AddScoped<ICalendarWebhookVerifier, CalendarWebhookVerifier>();
        services.AddScoped<ICalendarWebhookIntake, CalendarWebhookIntake>();
        services.AddScoped<ICalendarWebhookBindingResolver, CalendarWebhookBindingResolver>();
        services.AddScoped<CalendarWebhookVerifier>();
        services.AddScoped<CalendarWebhookIntake>();
        services.AddScoped<CalendarWebhookBindingResolver>();
        services.AddScoped<
            IRequestHandler<HandleCalendarWebhookCommand, Result>,
            HandleCalendarWebhookCommandHandler>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddSingleton<PipelineMetrics>();
        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid ConnectionId, Guid AccountId, Guid WorkspaceId)> SeedBindingAsync(string webhookPath)
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connection = IntegrationConnection.Create(
            accountId, workspaceId, IntegrationProvider.Google, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var binding = CalendarIntegration.Create(
            accountId, workspaceId, connection.Id, webhookPath,
            CalendarProvider.Google, CalendarSyncDirection.Pull, Guid.NewGuid(), DateTimeOffset.UtcNow);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.IntegrationConnections.Add(connection);
        seed.CalendarIntegrations.Add(binding);
        await seed.SaveChangesAsync();
        return (connection.Id, accountId, workspaceId);
    }

    private static (string Body, string Signature, string Timestamp) SignedCallback(string externalEventId)
    {
        var body = JsonSerializer.Serialize(new { eventId = externalEventId, kind = "calendar#event" });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return (body, signature, timestamp);
    }

    // ── polling helpers -------------------------------------------------------

    private async Task<long> OutboxRowCountAsync(Guid accountId, Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                          && m.AccountId == accountId
                          && m.WorkspaceId == workspaceId);
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid outboxId)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .AnyAsync(m => m.Id == outboxId && m.Status == "Processed");
        });
    }

    private async Task<bool> WaitForReceiptStatusAsync(Guid receiptId, string status)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == receiptId);
            return receipt?.Status == status;
        });
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    // ── tenant observation (mirrors the frozen scoped-tenant-chain pattern) ──

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }
        public bool ClearedAfterWorkspace { get; private set; }

        public void Reset()
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = false;
                ClearedAfterWorkspace = false;
            }
        }

        public void RecordWorkspace(Guid accountId, Guid workspaceId, bool isSystemContext)
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = true;
                LastWorkspaceAccountId = accountId;
                LastWorkspaceId = workspaceId;
                LastWorkspaceIsSystem = isSystemContext;
            }
        }

        public void RecordClear()
        {
            lock (_gate)
            {
                if (ObservedWorkspaceSet)
                {
                    ClearedAfterWorkspace = true;
                }
            }
        }
    }

    private sealed class RecordingCurrentTenantContext : ICurrentTenantContext
    {
        private readonly CurrentTenantContext _inner;
        private readonly TenantObservationRecorder _recorder;

        public RecordingCurrentTenantContext(CurrentTenantContext inner, TenantObservationRecorder recorder)
        {
            _inner = inner;
            _recorder = recorder;
        }

        public Guid? AccountId => _inner.AccountId;
        public Guid? WorkspaceId => _inner.WorkspaceId;
        public Guid? UserId => _inner.UserId;
        public bool IsSystemContext => _inner.IsSystemContext;
        public bool IsResolved => _inner.IsResolved;

        public Guid RequireAccountId() => _inner.RequireAccountId();
        public Guid RequireWorkspaceId() => _inner.RequireWorkspaceId();
        public Guid RequireUserId() => _inner.RequireUserId();

        public void SetUser(Guid userId) => _inner.SetUser(userId);
        public void SetAccountHint(Guid accountId) => _inner.SetAccountHint(accountId);

        public void SetAccount(Guid accountId, Guid? userId) => _inner.SetAccount(accountId, userId);

        public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId)
        {
            _inner.SetWorkspace(accountId, workspaceId, userId);
            if (!_inner.IsSystemContext)
            {
                _recorder.RecordWorkspace(accountId, workspaceId, _inner.IsSystemContext);
            }
        }

        public void SetSystem()
        {
            _inner.SetSystem();
        }

        public void Clear()
        {
            _inner.Clear();
            _recorder.RecordClear();
        }
    }
}