using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Placements.Commands.RebuildWorkspacePlacements;
using Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements;
using Notrelix.Application.Features.Analytics.Placements.Services;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Messaging.Consumers.Analytics;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// TAC-AR-001..012 — the Analytics-owned Work placement projection: live Work
/// facts update the projection (single producer-timestamp watermark),
/// duplicate/stale/out-of-order delivery cannot regress it, rebuild repairs
/// drift without overwriting a newer live fact, rows missing from a stale
/// snapshot are revalidated against the producer before deletion, concurrent
/// writes lose cleanly at the database (xmin) and converge on reload, and
/// workspace scopes stay isolated.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspacePlacementProjectionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspacePlacementProjectionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private (ApplicationDbContext Context, WorkspaceWorkItemPlacementService Service) CreateService(
        IWorkItemProjectionSourceAdapter? source = null)
    {
        var context = _db.CreateContext(SystemTenant());
        return (context, new WorkspaceWorkItemPlacementService(context, source ?? new StubProjectionSource()));
    }

    private static BoardItemMovedIntegrationEvent MovedEvent(
        Guid? accountId, Guid workspaceId, Guid itemId, Guid boardId, Guid groupId, DateTimeOffset occurredAt) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountId: accountId,
            ItemId: itemId,
            BoardId: boardId,
            WorkspaceId: workspaceId,
            OldGroupId: Guid.CreateVersion7(),
            NewGroupId: groupId,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: occurredAt);

    // ── live consumer projection semantics ───────────────────────────────

    [Fact]
    public async Task MovedFact_UpdatesProjection_LastWriteWins()
    {
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var firstGroup = Guid.CreateVersion7();
        var secondGroup = Guid.CreateVersion7();

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, firstGroup, false, Now, CancellationToken.None)).Should().BeTrue();
        await context.SaveChangesAsync();

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, secondGroup, false, Now.AddMinutes(1), CancellationToken.None)).Should().BeTrue();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(secondGroup);
        row.IsArchived.Should().BeFalse();
        row.SourceRevision.Should().Be(Now.AddMinutes(1).UtcTicks,
            "TAC-AR-012: the stored watermark is the single producer-timestamp scale");
    }

    [Fact]
    public async Task StaleOrDuplicateFact_DoesNotRegressProjection()
    {
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var currentGroup = Guid.CreateVersion7();
        var staleGroup = Guid.CreateVersion7();

        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, currentGroup, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, staleGroup, false, Now.AddSeconds(-30), CancellationToken.None))
            .Should().BeFalse("a stale fact must not regress the projection");
        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, staleGroup, false, Now, CancellationToken.None))
            .Should().BeFalse("a duplicate delivery at the same watermark must not change the projection");
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(currentGroup);
    }

    [Fact]
    public async Task WorkspaceScopes_StayIsolated()
    {
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceA = Guid.CreateVersion7();
        var workspaceB = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupA = Guid.CreateVersion7();
        var groupB = Guid.CreateVersion7();

        await service.ApplyPlacementAsync(accountId, workspaceA, itemId, boardId, groupA, false, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceB, itemId, boardId, groupB, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var rows = await context.WorkspaceWorkItemPlacements
            .Where(p => p.ItemId == itemId)
            .ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().Contain(p => p.WorkspaceId == workspaceA && p.GroupId == groupA);
        rows.Should().Contain(p => p.WorkspaceId == workspaceB && p.GroupId == groupB);
    }

    // ── rebuild semantics ────────────────────────────────────────────────

    [Fact]
    public async Task Rebuild_ReplacesProjectionFromProducerSnapshot_WithoutForeignAccess()
    {
        // Seed one Work item through the producer-owned persistence.
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Domain.Workspaces.Workspaces.Workspace.Create(accountId, ownerId, "AR WS", $"ar-{Guid.NewGuid():N}", Now);
        var board = Domain.WorkManagement.Boards.Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var group = Domain.WorkManagement.BoardGroups.BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Domain.SharedKernel.Color.Create("#808080"), Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);
        var item = Domain.WorkManagement.Items.BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Task", Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(item);
            await seed.SaveChangesAsync();
        }

        // Drift the projection with a wrong placement.
        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspace.Id, item.Id, board.Id, group.Id, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Rebuild from the producer-owned snapshot contract. The item exists in
        // the snapshot, so no per-row revalidation lookup occurs.
        var source = new StubProjectionSource(
            await new WorkItemProjectionSourceService(_db.CreateContext(SystemTenant()))
                .GetWorkspacePlacementsAsync(workspace.Id, CancellationToken.None));
        var (rebuildContext, rebuildService) = CreateService(source);
        await rebuildService.RebuildWorkspaceAsync(workspace.Id, source.WorkspaceSnapshots, CancellationToken.None);
        await rebuildContext.SaveChangesAsync();
        rebuildContext.ChangeTracker.Clear();

        var row = await rebuildContext.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspace.Id && p.ItemId == item.Id);
        row.GroupId.Should().Be(group.Id);
        row.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task Rebuild_OlderSnapshot_PreservesNewerLiveState()
    {
        // AR-FLOW-04: snapshot rev=10 while local is already rev=11 — no xmin
        // conflict is needed; the watermark guard itself must preserve the newer
        // live fact.
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var liveGroup = Guid.CreateVersion7();
        var staleGroup = Guid.CreateVersion7();

        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, liveGroup, false, Now.AddMinutes(1), CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var staleSnapshot = new List<WorkItemPlacementSnapshot>
        {
            new(accountId, itemId, boardId, staleGroup, false, 1, Now),
        };

        await service.RebuildWorkspaceAsync(workspaceId, staleSnapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(liveGroup, "a snapshot older than the local state must never overwrite a newer live fact");
        row.SourceRevision.Should().Be(Now.AddMinutes(1).UtcTicks);
    }

    [Fact]
    public async Task Rebuild_RowMissingFromSnapshot_SurvivesWhenProducerStillReportsIt()
    {
        // AR-FLOW-04: the live consumer projected a new item after the snapshot
        // was taken. Deletion requires producer proof the item is gone; the
        // revalidation lookup must keep (and repair) the row instead.
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var liveGroup = Guid.CreateVersion7();
        var producerGroup = Guid.CreateVersion7();

        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, liveGroup, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var source = new StubProjectionSource(
            [],
            itemFactory: (ws, item) => new WorkItemPlacementSnapshot(
                accountId, item, boardId, producerGroup, false, 1, Now.AddMinutes(1)));
        var (rebuildContext, rebuildService) = CreateService(source);

        await rebuildService.RebuildWorkspaceAsync(workspaceId, [], CancellationToken.None);
        await rebuildContext.SaveChangesAsync();
        rebuildContext.ChangeTracker.Clear();

        var row = await rebuildContext.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(producerGroup, "a row the snapshot lacks is reconciled to producer truth when the producer still reports the item");
    }

    [Fact]
    public async Task Rebuild_ConcurrentNewerLiveFact_ConflictsAtDatabase_AndConvergesOnReload()
    {
        // AR-FLOW-04: the rebuild transaction loaded the row before a live
        // consumer committed a newer fact. The stale rebuild write must fail at
        // the database (xmin row version) instead of silently overwriting, and
        // the reload+re-evaluate retry must converge on the newer live state.
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var rebuiltGroup = Guid.CreateVersion7();
        var liveGroup = Guid.CreateVersion7();

        var (staleContext, staleService) = CreateService();
        await staleService.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, Guid.CreateVersion7(), false, Now, CancellationToken.None);
        await staleContext.SaveChangesAsync();
        staleContext.ChangeTracker.Clear();

        await using var staleTransaction = await staleContext.Database.BeginTransactionAsync();
        var staleRow = await staleContext.WorkspaceWorkItemPlacements
            .SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);

        // A live fact commits on a separate transaction after the rebuild read.
        var (liveContext, liveService) = CreateService();
        (await liveService.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, liveGroup, false, Now.AddMinutes(1), CancellationToken.None))
            .Should().BeTrue();
        await liveContext.SaveChangesAsync();
        liveContext.ChangeTracker.Clear();
        await liveContext.DisposeAsync();

        // Equal watermark authorizes a drift repair — the payload change makes
        // the rebuild issue its UPDATE against the now-stale row version.
        staleRow.Reconcile(boardId, rebuiltGroup, false, Now).Should().BeTrue();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
        await staleTransaction.RollbackAsync();

        // Retry: reload re-evaluates the watermark and preserves the newer live fact.
        staleContext.ChangeTracker.Clear();
        await using (var retryTransaction = await staleContext.Database.BeginTransactionAsync())
        {
            await staleService.RebuildWorkspaceAsync(
                workspaceId,
                [new WorkItemPlacementSnapshot(accountId, itemId, boardId, rebuiltGroup, false, 1, Now)],
                CancellationToken.None);
            await staleContext.SaveChangesAsync();
            await retryTransaction.CommitAsync();
        }
        staleContext.ChangeTracker.Clear();

        var final = await staleContext.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        final.GroupId.Should().Be(liveGroup, "the older snapshot must not overwrite the committed newer live fact after reload");
        final.SourceRevision.Should().Be(Now.AddMinutes(1).UtcTicks);
    }

    [Fact]
    public async Task Rebuild_EmptySource_ClearsProjectionRows()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, groupId, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // The producer no longer reports the item (revalidation returns null) —
        // only then may the snapshot-missing row be deleted.
        await service.RebuildWorkspaceAsync(workspaceId, [], CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.WorkspaceWorkItemPlacements.AnyAsync(p => p.WorkspaceId == workspaceId))
            .Should().BeFalse("a rebuild from an empty source must remove rows only after producer revalidation confirms the item is gone");
    }

    [Fact]
    public async Task Rebuild_MultipleItems_ReconcilesAllAndRemovesMissing()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var keptItem = Guid.CreateVersion7();
        var movedItem = Guid.CreateVersion7();
        var removedItem = Guid.CreateVersion7();
        var groupA = Guid.CreateVersion7();
        var groupB = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, keptItem, boardId, groupA, false, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceId, movedItem, boardId, groupA, false, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceId, removedItem, boardId, groupA, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var snapshot = new List<WorkItemPlacementSnapshot>
        {
            new(accountId, keptItem, boardId, groupA, false, 1, Now),
            new(accountId, movedItem, boardId, groupB, false, 2, Now.AddMinutes(1)),
        };

        await service.RebuildWorkspaceAsync(workspaceId, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var rows = await context.WorkspaceWorkItemPlacements
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();
        rows.Should().HaveCount(2, "the removed item is gone from the producer (revalidated) and must be dropped");
        rows.Single(p => p.ItemId == keptItem).GroupId.Should().Be(groupA);
        rows.Single(p => p.ItemId == movedItem).GroupId.Should().Be(groupB);
    }

    [Fact]
    public async Task Rebuild_ArchivedItem_MatchesProducerTruth()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, groupId, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var snapshot = new List<WorkItemPlacementSnapshot>
        {
            new(accountId, itemId, boardId, groupId, true, 2, Now.AddMinutes(1)),
        };

        await service.RebuildWorkspaceAsync(workspaceId, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.IsArchived.Should().BeTrue("the rebuild must carry the archived producer truth");
    }

    [Fact]
    public async Task Rebuild_Duplicate_IsIdempotent()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var items = Enumerable.Range(0, 3)
            .Select(_ => Guid.CreateVersion7())
            .ToArray();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, items[0], boardId, groupId, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var snapshot = items
            .Select(item => new WorkItemPlacementSnapshot(accountId, item, boardId, groupId, false, 1, Now))
            .ToList();

        await service.RebuildWorkspaceAsync(workspaceId, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await service.RebuildWorkspaceAsync(workspaceId, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var rows = await context.WorkspaceWorkItemPlacements
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();
        rows.Should().HaveCount(items.Length, "repeated rebuild of the same snapshot must not duplicate rows");
    }

    [Fact]
    public async Task Rebuild_WorkspaceIsolation_LeavesOtherWorkspaceUntouched()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceA = Guid.CreateVersion7();
        var workspaceB = Guid.CreateVersion7();
        var itemA = Guid.CreateVersion7();
        var itemB = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupA = Guid.CreateVersion7();
        var groupB = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceA, itemA, boardId, groupA, false, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceB, itemB, boardId, groupB, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await service.RebuildWorkspaceAsync(workspaceA, [], CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.WorkspaceWorkItemPlacements.AnyAsync(p => p.WorkspaceId == workspaceA))
            .Should().BeFalse();
        var rowB = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceB && p.ItemId == itemB);
        rowB.GroupId.Should().Be(groupB, "a rebuild scoped to workspace A must not touch workspace B");
    }

    [Fact]
    public async Task Rebuild_ProducerUnavailable_FailsWithoutMutation_ThenRetryConverges()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var driftedGroup = Guid.CreateVersion7();
        var producerGroup = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, driftedGroup, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var snapshot = new List<WorkItemPlacementSnapshot>
        {
            new(accountId, itemId, boardId, producerGroup, false, 2, Now.AddMinutes(1)),
        };

        // Producer snapshot fetch fails BEFORE any mutation is staged.
        var unavailable = new StubProjectionSource(snapshot, throwOnFetch: true);
        var (failContext, failService) = CreateService(unavailable);
        var handler = new RebuildWorkspacePlacementsCommandHandler(unavailable, failService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new RebuildWorkspacePlacementsCommand(workspaceId), CancellationToken.None));
        failContext.ChangeTracker.Clear();
        var unchanged = await failContext.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        unchanged.GroupId.Should().Be(driftedGroup, "an unavailable producer snapshot must not mutate the projection");

        // Recovery: the same use case retried with an available producer converges.
        var available = new StubProjectionSource(snapshot);
        var (retryContext, retryService) = CreateService(available);
        var retryHandler = new RebuildWorkspacePlacementsCommandHandler(available, retryService);

        var result = await retryHandler.Handle(new RebuildWorkspacePlacementsCommand(workspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await retryContext.SaveChangesAsync();
        retryContext.ChangeTracker.Clear();
        var converged = await retryContext.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        converged.GroupId.Should().Be(producerGroup, "rebuild retry after producer recovery must converge to producer truth");
    }

    [Fact]
    public async Task Rebuild_HandlerUnchanged_FakeProducerContract_DrivesProjectedTruth()
    {
        // TAC-AR-001C: the WorkManagement producer implementation is substituted
        // with a fake of the frozen Public contract; the Analytics rebuild
        // handler, port and service run unchanged and project the fake's truth.
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var workspace = Domain.Workspaces.Workspaces.Workspace.Create(accountId, ownerId, "SUB WS", $"sub-{Guid.NewGuid():N}", Now);
        var board = Domain.WorkManagement.Boards.Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var realGroup = Domain.WorkManagement.BoardGroups.BoardGroup.Create(accountId, workspace.Id, board.Id, "Real", Domain.SharedKernel.Color.Create("#808080"), Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);
        var item = Domain.WorkManagement.Items.BoardItem.CreateRoot(accountId, workspace.Id, board.Id, realGroup.Id, "Task", Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(realGroup);
            seed.BoardItems.Add(item);
            await seed.SaveChangesAsync();
        }

        var fakeGroup = Guid.CreateVersion7();
        IWorkItemProjectionSource fakeProducer = new StubProjectionSource(
        [
            new WorkItemPlacementSnapshot(accountId, item.Id, board.Id, fakeGroup, false, 1, Now.AddMinutes(1)),
        ]);
        IWorkItemProjectionSourceAdapter analyticsPort =
            new Infrastructure.CrossContext.Analytics.WorkManagement.WorkItemProjectionSourceAdapter(fakeProducer);

        var (context, service) = CreateService(analyticsPort);
        var handler = new RebuildWorkspacePlacementsCommandHandler(analyticsPort, service);

        var result = await handler.Handle(new RebuildWorkspacePlacementsCommand(workspace.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspace.Id && p.ItemId == item.Id);
        row.GroupId.Should().Be(fakeGroup, "Analytics consumes whatever the producer contract reports — the producer implementation is replaceable");

        await using var probe = _db.CreateContext(SystemTenant());
        var workItem = await probe.BoardItems.SingleAsync(i => i.Id == item.Id);
        workItem.GroupId.Should().Be(realGroup.Id, "the substitution must not touch the real Work producer state");
    }

    [Fact]
    public async Task GetWorkspacePlacements_ProductionHandler_ReadsLocalScopedProjection()
    {
        // AR-FLOW-02 / TAC-AR-008 + TAC-AR-012: the production query handler
        // reads only the Analytics local projection, scoped by Workspace, and
        // tracks the canonical watermark.
        var (context, service) = CreateService();
        var accountId = Guid.CreateVersion7();
        var workspaceA = Guid.CreateVersion7();
        var workspaceB = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var itemA = Guid.CreateVersion7();
        var itemB = Guid.CreateVersion7();
        var groupA = Guid.CreateVersion7();
        var groupB = Guid.CreateVersion7();

        await service.ApplyPlacementAsync(accountId, workspaceA, itemA, boardId, groupA, false, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceB, itemB, boardId, groupB, false, Now.AddMinutes(1), CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new GetWorkspacePlacementsQueryHandler(context);
        var result = await handler.Handle(new GetWorkspacePlacementsQuery(workspaceA), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var placement = result.Data!.Should().ContainSingle().Subject;
        placement.ItemId.Should().Be(itemA);
        placement.GroupId.Should().Be(groupA);
        placement.LastOccurredAt.Should().Be(Now);
        placement.SourceRevision.Should().Be(Now.UtcTicks, "the query exposes the same single producer-timestamp watermark the projection was projected with");
    }

    [Fact]
    public async Task Rebuild_ThroughPublicItemPlacementContract_KeepsWorkAsProducerAuthority()
    {
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var workspace = Domain.Workspaces.Workspaces.Workspace.Create(accountId, ownerId, "WM05 WS", $"wm05-{Guid.NewGuid():N}", Now);
        var board = Domain.WorkManagement.Boards.Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var groupA = Domain.WorkManagement.BoardGroups.BoardGroup.Create(accountId, workspace.Id, board.Id, "A", Domain.SharedKernel.Color.Create("#808080"), Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);
        var groupB = Domain.WorkManagement.BoardGroups.BoardGroup.Create(accountId, workspace.Id, board.Id, "B", Domain.SharedKernel.Color.Create("#00FF00"), Domain.SharedKernel.Ordering.FractionalIndex.Create("a1"), ownerId, Now);
        var item = Domain.WorkManagement.Items.BoardItem.CreateRoot(accountId, workspace.Id, board.Id, groupA.Id, "Task", Domain.SharedKernel.Ordering.FractionalIndex.Initial(), ownerId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(groupA);
            seed.BoardGroups.Add(groupB);
            seed.BoardItems.Add(item);
            await seed.SaveChangesAsync();
        }

        // The producer-owned snapshot flows through the published contract,
        // implemented against the Work producer's own DbContext.
        IWorkItemProjectionSource producerSource =
            new WorkItemProjectionSourceService(_db.CreateContext(SystemTenant()));
        var snapshot = await producerSource.GetWorkspacePlacementsAsync(workspace.Id, CancellationToken.None);

        snapshot.Should().ContainSingle(s => s.ItemId == item.Id);
        var live = snapshot.Single(s => s.ItemId == item.Id);
        live.GroupId.Should().Be(groupA.Id, "the producer snapshot is the rebuild authority");

        // Analytics rebuilds its projection from that producer-owned snapshot.
        var source = new StubProjectionSource(snapshot);
        var (context, service) = CreateService(source);
        await service.ApplyPlacementAsync(accountId, workspace.Id, item.Id, board.Id, groupB.Id, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await service.RebuildWorkspaceAsync(workspace.Id, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspace.Id && p.ItemId == item.Id);
        row.GroupId.Should().Be(groupA.Id, "the rebuild restores the Work producer truth over drifted Analytics state");
        row.BoardId.Should().Be(board.Id);
    }

    // ── consumer paths ───────────────────────────────────────────────────

    [Fact]
    public async Task MovedConsumer_ProjectsFromEventFacts_WithoutProducerRead()
    {
        // TAC-AR-002: the moved payload already carries every placement fact;
        // the consumer composes only the projection service — a structural
        // fact additionally pinned by the boundary architecture test.
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        var consumer = new BoardItemMovedPlacementConsumer(
            service, new FakeCurrentTenantContext(), NullLogger<BoardItemMovedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemMovedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(MovedEvent(accountId, workspaceId, itemId, boardId, groupId, Now));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(groupId, "the event's own facts drive the move projection");
        row.AccountId.Should().Be(accountId);
        row.SourceRevision.Should().Be(Now.UtcTicks);
    }

    [Fact]
    public async Task MovedConsumer_MissingAccountIdPayload_UsesRestoredTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, Guid.CreateVersion7());

        var (context, service) = CreateService();
        var consumer = new BoardItemMovedPlacementConsumer(
            service, tenant, NullLogger<BoardItemMovedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemMovedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(MovedEvent(null, workspaceId, itemId, boardId, groupId, Now));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.AccountId.Should().Be(accountId, "a payload lacking account scope falls back to the tenant restored by the Platform runtime");
    }

    [Fact]
    public async Task CreatedConsumer_ProjectsFactThroughConsumerPath()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        IWorkItemProjectionSourceAdapter source = new StubProjectionSource(
            itemFactory: (ws, item) => new WorkItemPlacementSnapshot(
                accountId, item, boardId, groupId, IsArchived: false, Revision: 1, LastOccurredAt: Now));
        var consumer = new BoardItemCreatedPlacementConsumer(
            service, source, NullLogger<BoardItemCreatedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemCreatedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(
            new BoardItemCreatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                AccountId: accountId,
                ItemId: itemId,
                BoardId: boardId,
                WorkspaceId: workspaceId,
                Title: "Task",
                CorrelationId: Guid.CreateVersion7(),
                OccurredAt: Now));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(groupId);
        row.SourceRevision.Should().Be(Now.UtcTicks, "the fallback snapshot projects on the same producer-timestamp watermark");
    }

    [Fact]
    public async Task CreatedConsumer_SnapshotUnavailable_LeavesProjectionUnchanged()
    {
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        IWorkItemProjectionSourceAdapter source = new StubProjectionSource();
        var consumer = new BoardItemCreatedPlacementConsumer(
            service, source, NullLogger<BoardItemCreatedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemCreatedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(
            new BoardItemCreatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                AccountId: Guid.CreateVersion7(),
                ItemId: itemId,
                BoardId: Guid.CreateVersion7(),
                WorkspaceId: workspaceId,
                Title: "Task",
                CorrelationId: Guid.CreateVersion7(),
                OccurredAt: Now));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();

        (await context.WorkspaceWorkItemPlacements.AnyAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId))
            .Should().BeFalse("without a producer snapshot the created fact is not projectable — no placeholder mutation is made");
    }

    [Fact]
    public async Task ArchivedConsumer_ProjectsFactThroughConsumerPath()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, groupId, false, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var consumer = new BoardItemArchivedPlacementConsumer(
            service, NullLogger<BoardItemArchivedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemArchivedIntegrationEvent>>();
        var archivedAt = Now.AddMinutes(1);
        consumeContext.SetupGet(c => c.Message).Returns(
            new BoardItemArchivedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                AccountId: accountId,
                ItemId: itemId,
                BoardId: boardId,
                WorkspaceId: workspaceId,
                CorrelationId: Guid.CreateVersion7(),
                OccurredAt: archivedAt));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.IsArchived.Should().BeTrue("the archived fact must mark the projection row without deleting placement history");
        row.GroupId.Should().Be(groupId);
    }

    // ── test doubles ─────────────────────────────────────────────────────

    private sealed class StubProjectionSource : IWorkItemProjectionSourceAdapter, IWorkItemProjectionSource
    {
        public StubProjectionSource(
            IReadOnlyList<WorkItemPlacementSnapshot>? workspace = null,
            Func<Guid, Guid, WorkItemPlacementSnapshot?>? itemFactory = null,
            bool throwOnFetch = false)
        {
            WorkspaceSnapshots = workspace ?? [];
            _itemFactory = itemFactory ?? ((_, _) => null);
            ThrowOnFetch = throwOnFetch;
        }

        public IReadOnlyList<WorkItemPlacementSnapshot> WorkspaceSnapshots { get; }
        public bool ThrowOnFetch { get; set; }
        private readonly Func<Guid, Guid, WorkItemPlacementSnapshot?> _itemFactory;

        public Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
            Guid workspaceId, CancellationToken cancellationToken)
        {
            if (ThrowOnFetch)
                throw new InvalidOperationException("producer snapshot source unavailable");
            return Task.FromResult(WorkspaceSnapshots);
        }

        public Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
            Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
            => Task.FromResult(_itemFactory(workspaceId, itemId));
    }
}
