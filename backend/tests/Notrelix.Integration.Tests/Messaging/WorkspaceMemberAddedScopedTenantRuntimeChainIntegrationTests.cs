using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-FRZ-018 / PF-FLOW-07 — proves the required production runtime owner chain:
/// WorkspaceMember mutation -> outbox -> dispatcher -> MassTransit receive pipeline
/// -> TenantContextConsumeFilter -> DeduplicationConsumeFilter -> real activity
/// consumer -> Collaboration projection under the expected Workspace tenant.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string ActivityConsumerEndpoint = "notrelix-activity-member-added-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceMemberAddedScopedTenantRuntimeChainIntegrationTests(PostgresTestContainer db)
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
    public async Task WorkspaceMemberAdded_RunsThroughProductionDeliveryChainAndProjectsUnderWorkspaceTenant()
    {
        var graph = await SeedWorkspaceAsync();
        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            await AddWorkspaceMemberAsync(provider, graph);
            recorder.Reset();

            var outbox = await WaitForOutboxAsync(graph);
            outbox.Should().NotBeNull();

            outbox!.AccountId.Should().Be(graph.AccountId);
            outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
            outbox.MessageName.Should().Be("workspace.member.added");
            outbox.SchemaVersion.Should().Be(1);
            outbox.PayloadJson.RootElement.GetProperty("accountId").GetGuid().Should().Be(graph.AccountId);
            outbox.PayloadJson.RootElement.GetProperty("workspaceId").GetGuid().Should().Be(graph.WorkspaceId);

            var activity = await WaitForActivityAsync(outbox.EventId, graph.WorkspaceId);
            activity.Should().NotBeNull();

            var dispatcherCompleted = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatcherCompleted.Should().BeTrue();

            var dedupCompleted = await WaitForDedupSucceededAsync(outbox.EventId, ActivityConsumerEndpoint);
            dedupCompleted.Should().BeTrue();

            await using var probe = _db.CreateContext(SystemTenant());
            (await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .CountAsync(m => m.MessageName == "workspace.member.added"
                    && m.WorkspaceId == graph.WorkspaceId)).Should().Be(1);

            (await probe.Set<WorkspaceActivityLogRecord>()
                .IgnoreQueryFilters()
                .CountAsync(a => a.SourceEventId == outbox.EventId)).Should().Be(1,
                "the real production consumer must project the committed membership fact exactly once");

            (await probe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.EventId == outbox.EventId
                    && p.ConsumerName == "OutboxDispatcher"
                    && p.Status == "Succeeded")).Should().BeTrue();
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before the consumer pipe runs");
        recorder.LastWorkspaceAccountId.Should().Be(graph.AccountId);
        recorder.LastWorkspaceId.Should().Be(graph.WorkspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse();

        recorder.SaveChangesAccountId.Should().Be(graph.AccountId,
            "the real activity consumer must persist while the scoped tenant context is Account-scoped");
        recorder.SaveChangesWorkspaceId.Should().Be(graph.WorkspaceId,
            "the real activity consumer must persist while the scoped tenant context is Workspace-scoped");
        recorder.SaveChangesIsSystem.Should().BeFalse(
            "a Workspace-scoped integration event must not execute its consumer as System");

        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    private async Task<MembershipGraph> SeedWorkspaceAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var memberUserId = Guid.CreateVersion7();

        var owner = User.Create($"chain-owner-{Guid.NewGuid():N}@example.com", "Chain Owner", "hashed", now, true);
        var member = User.Create($"chain-member-{Guid.NewGuid():N}@example.com", "Chain Member", "hashed", now, true);
        var workspace = Workspace.Create(accountId, ownerId, "Scoped Tenant Chain", $"chain-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        await seed.SaveChangesAsync();

        return new MembershipGraph(accountId, workspace.Id, memberUserId, ownerId);
    }

    private async Task AddWorkspaceMemberAsync(ServiceProvider provider, MembershipGraph graph)
    {
        await using var scope = provider.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
        tenant.SetSystem();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            graph.AccountId,
            graph.WorkspaceId,
            graph.MemberUserId,
            WorkspaceRole.Member,
            graph.ActorUserId,
            DateTimeOffset.UtcNow));

        await context.SaveChangesAsync();
    }

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

        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddObservability(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();

        services.AddScoped<IActivityProjectionDbContext>(sp =>
            new ActivityProjectionTenantProbe(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<ICurrentTenantContext>(),
                recorder));

        return services.BuildServiceProvider();
    }

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(MembershipGraph graph)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == "workspace.member.added"
                    && m.AccountId == graph.AccountId
                    && m.WorkspaceId == graph.WorkspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<WorkspaceActivityLogRecord?> WaitForActivityAsync(Guid sourceEventId, Guid workspaceId)
    {
        WorkspaceActivityLogRecord? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<WorkspaceActivityLogRecord>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.SourceEventId == sourceEventId && a.WorkspaceId == workspaceId);
            return found is not null;
        });

        return completed ? found : null;
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

    private async Task<bool> WaitForDedupSucceededAsync(Guid eventId, string consumerName)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.EventId == eventId
                    && p.ConsumerName == consumerName
                    && p.Status == "Succeeded");
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

    private sealed record MembershipGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid MemberUserId,
        Guid ActorUserId);

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }
        public bool ClearedAfterWorkspace { get; private set; }

        public Guid? SaveChangesAccountId { get; private set; }
        public Guid? SaveChangesWorkspaceId { get; private set; }
        public bool SaveChangesIsSystem { get; private set; }

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

        public void RecordSaveChanges(Guid? accountId, Guid? workspaceId, bool isSystemContext)
        {
            lock (_gate)
            {
                SaveChangesAccountId = accountId;
                SaveChangesWorkspaceId = workspaceId;
                SaveChangesIsSystem = isSystemContext;
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = false;
                LastWorkspaceAccountId = null;
                LastWorkspaceId = null;
                LastWorkspaceIsSystem = false;
                ClearedAfterWorkspace = false;
                SaveChangesAccountId = null;
                SaveChangesWorkspaceId = null;
                SaveChangesIsSystem = false;
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
            _recorder.RecordWorkspace(accountId, workspaceId, _inner.IsSystemContext);
        }

        public void SetSystem() => _inner.SetSystem();

        public void Clear()
        {
            _inner.Clear();
            _recorder.RecordClear();
        }
    }

    private sealed class ActivityProjectionTenantProbe : IActivityProjectionDbContext
    {
        private readonly ApplicationDbContext _inner;
        private readonly ICurrentTenantContext _tenant;
        private readonly TenantObservationRecorder _recorder;

        public ActivityProjectionTenantProbe(
            ApplicationDbContext inner,
            ICurrentTenantContext tenant,
            TenantObservationRecorder recorder)
        {
            _inner = inner;
            _tenant = tenant;
            _recorder = recorder;
        }

        public DbSet<WorkspaceActivityLogRecord> WorkspaceActivityLogs => _inner.WorkspaceActivityLogs;
        public DbSet<ActivityReadStateRecord> ActivityReadStates => _inner.ActivityReadStates;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            _recorder.RecordSaveChanges(_tenant.AccountId, _tenant.WorkspaceId, _tenant.IsSystemContext);
            return _inner.SaveChangesAsync(cancellationToken);
        }
    }
}
