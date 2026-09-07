using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
/// TAC-WM-009 + TAC-FRZ-018 — Work integration-event runtime ownership:
/// a valid Work mutation enrolls exactly one board_item.moved fact in the
/// same committed transaction, a rolled-back mutation enrolls none, and the
/// production delivery chain (outbox dispatcher → MassTransit receive pipeline
/// → TenantContextConsumeFilter → real Analytics placement consumer) restores
/// the Workspace tenant before consuming and projects the placement exactly
/// once under that tenant.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardItemMovedOutboxRuntimeTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardItemMovedOutboxRuntimeTests(PostgresTestContainer db)
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
    public async Task ValidMove_CommitsExactlyOneScopedFact_AndConsumerProjectsUnderRestoredTenant()
    {
        var graph = await SeedBoardStackAsync();
        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        // 1. Valid Work mutation + commit: the Domain fact and the outbox row
        //    must land in the same committed transaction.
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

        var outbox = await WaitForOutboxAsync(graph);
        outbox.Should().NotBeNull("the committed mutation must enroll its fact in the outbox");

        outbox!.AccountId.Should().Be(graph.AccountId, "TAC-FRZ-018: authoritative tenant envelope");
        outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
        outbox.MessageName.Should().Be("board_item.moved");
        outbox.SchemaVersion.Should().Be(1);
        outbox.PayloadJson.RootElement.GetProperty("itemId").GetGuid().Should().Be(graph.ItemId);
        outbox.PayloadJson.RootElement.GetProperty("oldGroupId").GetGuid().Should().Be(graph.SourceGroupId);
        outbox.PayloadJson.RootElement.GetProperty("newGroupId").GetGuid().Should().Be(graph.TargetGroupId);
        outbox.PayloadJson.RootElement.GetProperty("accountId").GetGuid().Should().Be(graph.AccountId);
        outbox.PayloadJson.RootElement.GetProperty("workspaceId").GetGuid().Should().Be(graph.WorkspaceId);
        outbox.PayloadJson.RootElement.GetProperty("actorUserId").GetGuid().Should().Be(graph.ExecutorUserId);

        // 2. Start the real delivery chain and let the consumer project.
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            recorder.Reset();

            var dispatched = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatched.Should().BeTrue("the dispatcher must deliver the committed fact");

            var projected = await WaitForPlacementAsync(graph);
            projected.Should().BeTrue(
                "the real Analytics placement consumer must project the moved item under the restored tenant");

            (await OutboxRowCountAsync(graph)).Should().Be(1,
                "TAC-WM-009: exactly one outward fact enrollment per move");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before consumers run");
        recorder.LastWorkspaceAccountId.Should().Be(graph.AccountId);
        recorder.LastWorkspaceId.Should().Be(graph.WorkspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse(
            "a Workspace-scoped Work fact must not run its consumers as System");
        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    [Fact]
    public async Task RolledBackMove_EnrollsNoOutboxFact()
    {
        var graph = await SeedBoardStackAsync();
        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder);

        // Mutation inside an explicit transaction that is rolled back: neither
        // the Work change nor the enrolled fact may survive.
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetWorkspace(graph.AccountId, graph.WorkspaceId, graph.ExecutorUserId);
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var tx = await context.Database.BeginTransactionAsync();
            var item = await context.BoardItems.FirstAsync(i => i.Id == graph.ItemId);
            var targetGroup = await context.BoardGroups.FirstAsync(g => g.Id == graph.TargetGroupId);
            item.MoveToGroup(
                BoardGroupRef.From(targetGroup),
                FractionalIndexGenerator.GenerateKeyBetween(null, null),
                graph.ExecutorUserId,
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        var rolledBack = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .AnyAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId);
        }, timeoutSeconds: 3);

        rolledBack.Should().BeFalse(
            "TAC-WM-009: a failed/rolled-back Work mutation must enroll no outward event");

        await using var verify = _db.CreateContext(SystemTenant());
        var persisted = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        persisted.GroupId.Should().Be(graph.SourceGroupId);
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
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<BoardGraph> SeedBoardStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.CreateVersion7();
        var executorUser = User.Create($"wm-ev-{Guid.NewGuid():N}@example.com", "WM Executor", "hashed", now, true);
        var workspace = Workspace.Create(accountId, executorUser.Id, "WM Events WS", $"wm-ev-{Guid.NewGuid():N}", now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, executorUser.Id, WorkspaceRole.Member, executorUser.Id, now);
        var board = Board.Create(accountId, workspace.Id, executorUser.Id, "Events Board", null, now);
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

    // ── polling helpers -------------------------------------------------------

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(BoardGraph graph)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<long> OutboxRowCountAsync(BoardGraph graph)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId);
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