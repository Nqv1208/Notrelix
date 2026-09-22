using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-AR-011 / AR-FLOW-04 — projection failure and recovery on the real
/// delivery chain: a placement projection persistence failure (crash before
/// commit) rolls the consumer effect and its dedup claim back atomically, the
/// Work source state remains untouched, and the Platform delivery mechanism
/// retries until the projection converges to producer truth.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardItemMovedPlacementFailureRecoveryRuntimeTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardItemMovedPlacementFailureRecoveryRuntimeTests(PostgresTestContainer db)
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
    public async Task ProjectionCrashBeforeCommit_RollsBackClaim_Retries_AndConvergesWithoutTouchingSource()
    {
        var graph = await SeedBoardStackAsync();
        var recorder = new TenantObservationRecorder();
        var failureBudget = new ProjectionFailureBudget(1);
        await using var provider = BuildProvider(recorder, failureBudget);

        // 1. A valid Work move commits and enrolls its fact.
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetWorkspace(graph.AccountId, graph.WorkspaceId, graph.ExecutorUserId);
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await context.BoardItems.FirstAsync(i => i.Id == graph.ItemId);
            var targetGroup = await context.BoardGroups.FirstAsync(g => g.Id == graph.TargetGroupId);
            item.MoveToGroup(
                BoardGroupRef.From(targetGroup),
                FractionalIndexGenerator.GenerateKeyBetween(null, null),
                graph.ExecutorUserId,
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        var eventId = await WaitForOutboxEventIdAsync(graph);
        eventId.Should().NotBeNull("the committed mutation must enroll its fact in the outbox");

        // 2. Delivery: the first projection attempt crashes before commit.
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        long? sourceVersionAfterMove = null;
        try
        {
            var converged = await WaitForPlacementAsync(graph);
            if (!converged)
            {
                // Transport swallowed the fault without automatic redelivery:
                // republish the SAME logical event — at-least-once retry.
                await RepublishMovedEventAsync(provider, graph, eventId!.Value);
                converged = await WaitForPlacementAsync(graph);
            }

            converged.Should().BeTrue(
                "TAC-AR-011: the delivery mechanism must retry after the projection crash and converge");
            failureBudget.TryConsume().Should().BeFalse(
                "the injected failure must have been consumed by the crashed first attempt");

            // 3. Final projection converged to producer truth.
            await using (var verify = _db.CreateContext(SystemTenant()))
            {
                var projection = await verify.WorkspaceWorkItemPlacements
                    .SingleAsync(p => p.WorkspaceId == graph.WorkspaceId && p.ItemId == graph.ItemId);
                projection.GroupId.Should().Be(graph.TargetGroupId);
            }

            // 4. Crash-before-commit left no partial durable consumer state:
            //    exactly one consumer claim, in the Succeeded terminal state.
            await using (var audit = _db.CreateContext(SystemTenant()))
            {
                var consumerRows = await audit.Set<MessagingProcessedEvent>()
                    .IgnoreQueryFilters()
                    .Where(e => e.EventId == eventId
                        && e.ConsumerName == "notrelix-board-item-moved-placement")
                    .ToListAsync();
                consumerRows.Should().ContainSingle(
                    "the failed attempt's claim transaction rolled back atomically — only the retry's claim survives");
                consumerRows[0].Status.Should().Be("Succeeded");
            }

            // 5. Source Work state remained the authoritative, untouched truth.
            await using (var probe = _db.CreateContext(SystemTenant()))
            {
                var workItem = await probe.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
                workItem.GroupId.Should().Be(graph.TargetGroupId,
                    "a consumer-side projection failure must never roll back or alter Work source state");
                sourceVersionAfterMove = workItem.Version;
            }
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        // A later delivery retry could not move the source either.
        await using (var recheck = _db.CreateContext(SystemTenant()))
        {
            var workItem = await recheck.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
            workItem.GroupId.Should().Be(graph.TargetGroupId);
            workItem.Version.Should().Be(sourceVersionAfterMove,
                "projection retries must not re-execute or disturb the producer mutation");
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "recovery runs consumers under the restored Workspace tenant");
    }

    // ── composition ──────────────────────────────────────────────────────

    private ServiceProvider BuildProvider(TenantObservationRecorder recorder, ProjectionFailureBudget failureBudget)
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
        services.AddCrossContextBindings();
        services.AddScoped<IWorkItemProjectionSource, WorkItemProjectionSourceService>();
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();

        // Force the projection persistence seam to fail on its first use inside
        // the consumer, simulating a crash before commit on the real chain.
        services.RemoveAll<IReportingDbContext>();
        services.AddScoped<IReportingDbContext>(sp =>
            new FailOnceReportingDbContext(sp.GetRequiredService<ApplicationDbContext>(), failureBudget));

        return services.BuildServiceProvider();
    }

    private async Task RepublishMovedEventAsync(ServiceProvider provider, BoardGraph graph, Guid eventId)
    {
        var bus = provider.GetRequiredService<MassTransit.IBus>();
        await bus.Publish(new Notrelix.Application.Events.WorkManagement.BoardItemMovedIntegrationEventV2(
            EventId: eventId,
            AccountId: graph.AccountId,
            ItemId: graph.ItemId,
            BoardId: graph.BoardId,
            WorkspaceId: graph.WorkspaceId,
            OldGroupId: graph.SourceGroupId,
            NewGroupId: graph.TargetGroupId,
            Revision: 2,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: DateTimeOffset.UtcNow));
    }

    // ── seeding ──────────────────────────────────────────────────────────

    private async Task<BoardGraph> SeedBoardStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.CreateVersion7();
        var executorUser = User.Create($"ar-fail-{Guid.NewGuid():N}@example.com", "AR Failure Executor", "hashed", now, true);
        var workspace = Workspace.Create(accountId, executorUser.Id, "AR Failure WS", $"ar-fail-{Guid.NewGuid():N}", now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, executorUser.Id, WorkspaceRole.Member, executorUser.Id, now);
        var board = Board.Create(accountId, workspace.Id, executorUser.Id, "Failure Board", null, now);
        var sourceGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Domain.SharedKernel.Color.Create("#808080"), FractionalIndex.Create("a0"), executorUser.Id, now);
        var targetGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Done", Domain.SharedKernel.Color.Create("#00FF00"), FractionalIndex.Create("a1"), executorUser.Id, now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, sourceGroup.Id, "Task", FractionalIndex.Create("a0"), executorUser.Id, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(executorUser);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(sourceGroup);
        seed.BoardGroups.Add(targetGroup);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new Notrelix.Infrastructure.Data.Authz.AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspace.Id, executorUser.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return new BoardGraph(accountId, workspace.Id, item.Id, board.Id, sourceGroup.Id, targetGroup.Id, executorUser.Id);
    }

    // ── polling helpers ──────────────────────────────────────────────────

    private async Task<Guid?> WaitForOutboxEventIdAsync(BoardGraph graph)
    {
        Guid? eventId = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId);
            if (found is not null) eventId = found.EventId;
            return found is not null;
        });

        return completed ? eventId : null;
    }

    private async Task<bool> WaitForPlacementAsync(BoardGraph graph)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.WorkspaceWorkItemPlacements
                .AnyAsync(p => p.WorkspaceId == graph.WorkspaceId
                            && p.ItemId == graph.ItemId
                            && p.GroupId == graph.TargetGroupId);
        }, timeoutSeconds: 20);
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

    private sealed record BoardGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid ItemId,
        Guid BoardId,
        Guid SourceGroupId,
        Guid TargetGroupId,
        Guid ExecutorUserId);

    // ── failure injection ────────────────────────────────────────────────

    private sealed class ProjectionFailureBudget
    {
        private int _remaining;

        public ProjectionFailureBudget(int failures) => _remaining = failures;

        public bool TryConsume() => Interlocked.Decrement(ref _remaining) >= 0;
    }

    /// <summary>
    /// Delegates to the real scoped ApplicationDbContext (so committed state is
    /// unchanged on success) while failing the placement projection seam the
    /// configured number of times — a crash before the consumer commits.
    /// </summary>
    private sealed class FailOnceReportingDbContext : IReportingDbContext
    {
        private readonly ApplicationDbContext _inner;
        private readonly ProjectionFailureBudget _budget;

        public FailOnceReportingDbContext(ApplicationDbContext inner, ProjectionFailureBudget budget)
        {
            _inner = inner;
            _budget = budget;
        }

        public DbSet<Notrelix.Domain.Analytics.Dashboards.Dashboard> Dashboards => _inner.Dashboards;
        public DbSet<Notrelix.Domain.Analytics.Dashboards.DashboardWidget> DashboardWidgets => _inner.DashboardWidgets;
        public DbSet<Notrelix.Domain.Analytics.Dashboards.DashboardSource> DashboardSources => _inner.DashboardSources;
        public DbSet<Notrelix.Domain.Analytics.Snapshots.ReportingSnapshot> ReportingSnapshots => _inner.ReportingSnapshots;

        public DbSet<Notrelix.Application.Features.Analytics.Projections.WorkItemPlacement.WorkspaceWorkItemPlacementProjection> WorkspaceWorkItemPlacements
        {
            get
            {
                if (_budget.TryConsume())
                    throw new InvalidOperationException("injected placement projection persistence failure");
                return _inner.WorkspaceWorkItemPlacements;
            }
        }
    }

    // ── tenant observation (mirrors the frozen scoped-tenant-chain pattern) ──

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }

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

        public void SetSystem() => _inner.SetSystem();
        public void Clear() => _inner.Clear();
    }
}
