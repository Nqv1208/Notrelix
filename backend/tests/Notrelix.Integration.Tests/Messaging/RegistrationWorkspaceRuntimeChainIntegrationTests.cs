using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-IA-FLOW-03 — full production runtime chain proof: registration →
/// IdentityRegistrationCompleted outbox → dispatcher → MassTransit receive
/// pipeline → TenantContextConsumeFilter → DeduplicationConsumeFilter →
/// WorkspaceProvisioningConsumer → ProvisionPersonalWorkspaceCommand → personal
/// Workspace + Owner member persisted under the Account tenant.
///
/// Mirrors WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests
/// (the scoped runtime-chain pattern the flow card references).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class RegistrationWorkspaceRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string WorkspaceProvisionEndpoint = "notrelix-identity-registration-completed-workspace-provision-v1";
    private const string RegistrationMessageName = "identity.registration-completed";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RegistrationWorkspaceRuntimeChainIntegrationTests(PostgresTestContainer db)
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
    public async Task Registration_RunsThroughProductionChainAndProvisionsPersonalWorkspaceUnderAccountTenant()
    {
        var graph = new RegistrationGraph(Guid.CreateVersion7(), Guid.CreateVersion7());
        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            await RegisterAsync(provider, graph);
            recorder.Reset();

            var outbox = await WaitForOutboxAsync(graph.AccountId);
            outbox.Should().NotBeNull();
            outbox!.MessageName.Should().Be(RegistrationMessageName);
            outbox.AccountId.Should().Be(graph.AccountId);
            outbox.PayloadJson.RootElement.GetProperty("accountId").GetGuid().Should().Be(graph.AccountId);

            var workspace = await WaitForPersonalWorkspaceAsync(graph.AccountId);

            if (workspace is null)
            {
                await using var diag = _db.CreateContext(SystemTenant());
                var outboxState = await diag.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                    .Where(m => m.Id == outbox.Id).Select(m => new { m.Status, m.RetryCount }).ToListAsync();
                var dedupState = await diag.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
                    .Where(p => p.EventId == outbox.EventId).Select(p => new { p.ConsumerName, p.Status, p.ErrorMessage }).ToListAsync();
                var attempts = await diag.Set<OutboxDeliveryAttempt>().IgnoreQueryFilters()
                    .Where(a => a.OutboxMessageId == outbox.Id).Select(a => new { a.Status, a.ErrorCode, a.ErrorMessage }).ToListAsync();
                throw new Xunit.Sdk.XunitException(
                    "personal Workspace was not provisioned. outbox=" +
                    System.Text.Json.JsonSerializer.Serialize(outboxState) +
                    " processed=" + System.Text.Json.JsonSerializer.Serialize(dedupState) +
                    " attempts=" + System.Text.Json.JsonSerializer.Serialize(attempts));
            }

            workspace.Should().NotBeNull("the real production consumer must provision the personal Workspace");

            var dedupCompleted = await WaitForDedupSucceededAsync(outbox.EventId, WorkspaceProvisionEndpoint);
            dedupCompleted.Should().BeTrue();

            var dispatcherCompleted = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatcherCompleted.Should().BeTrue();

            await using var probe = _db.CreateContext(SystemTenant());
            (await probe.Workspaces.IgnoreQueryFilters()
                .CountAsync(w => w.AccountId == graph.AccountId && w.IsPersonal)).Should().Be(1,
                "exactly one personal Workspace must be provisioned");
            (await probe.WorkspaceMembers.IgnoreQueryFilters()
                .AnyAsync(m => m.WorkspaceId == workspace!.Id && m.UserId == graph.UserId)).Should().BeTrue(
                "the registered user must be the Owner WorkspaceMember");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedAccountSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Account tenant before the consumer pipe runs");
        recorder.LastAccountId.Should().Be(graph.AccountId);
        recorder.LastIsSystem.Should().BeFalse();
    }

    private async Task<Guid> RegisterAsync(ServiceProvider provider, RegistrationGraph graph)
    {
        var email = $"chain-reg-{Guid.NewGuid():N}@example.com";
        await using var scope = provider.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
        tenant.SetSystem();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var collector = scope.ServiceProvider.GetRequiredService<IIntegrationEventCollector>();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");
        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid?>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var sessionIssuer = new AuthSessionIssuer(
            jwtService.Object, context, dateTimeProvider.Object, new Mock<IClientMetadata>().Object);

        var provisioning = new AccountProvisioningService(
            context,
            new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));

        var handler = new RegisterCommandHandler(
            context,
            provisioning,
            passwordHasher.Object,
            sessionIssuer,
            dateTimeProvider.Object,
            collector);

        var result = await handler.Handle(new RegisterCommand
        {
            Email = email,
            Password = "Password1!",
            Name = "Chain Registration User"
        }, CancellationToken.None);
        result.Succeeded.Should().BeTrue($"Handle Succeeded=false. Errors: {string.Join(", ", result.Errors)}");

        await context.SaveChangesAsync();

        var user = await context.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == email.ToLowerInvariant());
        var account = await context.Accounts.IgnoreQueryFilters()
            .SingleAsync(a => a.Type == Notrelix.Domain.Accounts.Accounts.AccountType.Personal);
        graph.SetUser(user.Id);
        graph.SetAccount(account.Id);
        return account.Id;
    }

    private ServiceProvider BuildProvider(TenantObservationRecorder recorder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["Messaging:Transport"] = "InMemory",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
                ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "notrelix-test",
                ["JwtSettings:Audience"] = "notrelix-test",
                ["JwtSettings:ExpireMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpireDays"] = "7",
            })
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["Messaging:Transport"] = "InMemory",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
            ["JwtSettings:Issuer"] = "notrelix-test",
            ["JwtSettings:Audience"] = "notrelix-test",
            ["JwtSettings:ExpireMinutes"] = "30",
            ["JwtSettings:RefreshTokenExpireDays"] = "7",
        });

        builder.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Information));
        builder.Services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        builder.Services.AddSingleton(environment.Object);

        // Full production Infrastructure composition (exactly what Program.cs wires) so
        // the consumer's real MediatR command pipeline — ExecutionContext, AccessControl,
        // Idempotency, DataSession behaviors — resolves every dependency
        // (ICurrentCredentialContext, IIdempotencyStore, IEmailOutboxWriter, ...) just as
        // in production. Granular registration would silently omit these.
        builder.Services.AddInfrastructure(configuration, environment.Object);

        // Deliberate host-boundary overrides registered AFTER AddInfrastructure so they
        // win in the resolved graph (DI: last registration wins for GetService<T>).
        builder.Services.AddScoped<ICurrentTenantContext>(_ =>
            new RecordingCurrentTenantContext(new CurrentTenantContext(), recorder));
        builder.Services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser());

        builder.Services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();

        builder.AddApplicationServices();

        return builder.Services.BuildServiceProvider();
    }

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(Guid accountId)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == RegistrationMessageName
                    && m.AccountId == accountId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<Workspace?> WaitForPersonalWorkspaceAsync(Guid accountId)
    {
        Workspace? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Workspaces.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.AccountId == accountId && w.IsPersonal);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<bool> WaitForDedupSucceededAsync(Guid eventId, string consumerEndpoint)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.EventId == eventId
                    && p.ConsumerName == consumerEndpoint
                    && p.Status == "Succeeded");
        });
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid outboxId)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == outboxId && m.Status == "Processed");
        });
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 40)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(250);
        }

        return false;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class RegistrationGraph
    {
        public RegistrationGraph(Guid accountId, Guid? userId = null)
        {
            AccountId = accountId;
            UserId = userId;
        }

        public Guid AccountId { get; private set; }
        public Guid? UserId { get; private set; }

        public void SetAccount(Guid accountId) => AccountId = accountId;
        public void SetUser(Guid userId) => UserId = userId;
    }

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedAccountSet { get; private set; }
        public Guid? LastAccountId { get; private set; }
        public bool LastIsSystem { get; private set; }

        public void RecordAccount(Guid accountId, bool isSystemContext)
        {
            lock (_gate)
            {
                ObservedAccountSet = true;
                LastAccountId = accountId;
                LastIsSystem = isSystemContext;
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                ObservedAccountSet = false;
                LastAccountId = null;
                LastIsSystem = false;
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
        public void SetAccount(Guid accountId, Guid? userId)
        {
            _inner.SetAccount(accountId, userId);
            _recorder.RecordAccount(accountId, _inner.IsSystemContext);
        }

        public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId) => _inner.SetWorkspace(accountId, workspaceId, userId);
        public void SetSystem() => _inner.SetSystem();
        public void Clear() => _inner.Clear();
    }
}
