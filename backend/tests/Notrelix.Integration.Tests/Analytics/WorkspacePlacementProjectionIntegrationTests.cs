using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Analytics.Placements.Services;
using Notrelix.Application.Features.WorkManagement.Public.Queries;
using Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Messaging.Consumers.Analytics;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// TAC-AR-001..009 — the Analytics-owned Work placement projection:
/// live Work facts update the projection (last-write-wins by producer
/// timestamp), duplicate/stale delivery cannot regress it, rebuild replaces
/// drift from the producer-owned snapshot without foreign table access, and
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

    private (ApplicationDbContext Context, WorkspaceWorkItemPlacementService Service) CreateService()
    {
        var context = _db.CreateContext(SystemTenant());
        return (context, new WorkspaceWorkItemPlacementService(context));
    }

    private static BoardItemMovedIntegrationEvent MovedEvent(
        Guid workspaceId, Guid itemId, Guid boardId, Guid groupId, DateTimeOffset occurredAt) =>
        new(
            Guid.CreateVersion7(), itemId, boardId, workspaceId,
            OldGroupId: Guid.CreateVersion7(), NewGroupId: groupId,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: occurredAt);

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

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, firstGroup, false, Now.UtcTicks, Now, CancellationToken.None)).Should().BeTrue();
        await context.SaveChangesAsync();

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, secondGroup, false, Now.AddMinutes(1).UtcTicks, Now.AddMinutes(1), CancellationToken.None)).Should().BeTrue();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(secondGroup);
        row.IsArchived.Should().BeFalse();
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

        await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, currentGroup, false, Now.UtcTicks, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await service.ApplyPlacementAsync(accountId, workspaceId, itemId, boardId, staleGroup, false, Now.AddSeconds(-30).UtcTicks, Now.AddSeconds(-30), CancellationToken.None))
            .Should().BeFalse("a stale fact must not regress the projection");
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

        await service.ApplyPlacementAsync(accountId, workspaceA, itemId, boardId, groupA, false, Now.UtcTicks, Now, CancellationToken.None);
        await service.ApplyPlacementAsync(accountId, workspaceB, itemId, boardId, groupB, false, Now.UtcTicks, Now, CancellationToken.None);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var rows = await context.WorkspaceWorkItemPlacements
            .Where(p => p.ItemId == itemId)
            .ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().Contain(p => p.WorkspaceId == workspaceA && p.GroupId == groupA);
        rows.Should().Contain(p => p.WorkspaceId == workspaceB && p.GroupId == groupB);
    }

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
        await service.ApplyPlacementAsync(accountId, workspace.Id, item.Id, board.Id, group.Id, false, 1, Now, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Rebuild from the producer-owned snapshot contract.
        var source = new WorkItemProjectionSourceAdapter(_db.CreateContext(SystemTenant()));
        var snapshot = await source.GetWorkspacePlacementsAsync(workspace.Id, CancellationToken.None);
        await service.RebuildWorkspaceAsync(workspace.Id, snapshot, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspace.Id && p.ItemId == item.Id);
        row.GroupId.Should().Be(group.Id);
        row.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task MovedConsumer_ProjectsFactThroughConsumerPath()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var (context, service) = CreateService();
        var projectionSourceMock = new Mock<IWorkItemProjectionSourceAdapter>();
        projectionSourceMock
            .Setup(s => s.GetItemPlacementAsync(workspaceId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemPlacementSnapshot(
                accountId, itemId, boardId, groupId, IsArchived: false,
                Revision: Now.UtcTicks, LastOccurredAt: Now));
        var consumer = new BoardItemMovedPlacementConsumer(
            service, projectionSourceMock.Object, NullLogger<BoardItemMovedPlacementConsumer>.Instance);
        var consumeContext = new Mock<MassTransit.ConsumeContext<BoardItemMovedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(MovedEvent(workspaceId, itemId, boardId, groupId, Now));
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.WorkspaceWorkItemPlacements.SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
        row.GroupId.Should().Be(groupId);
    }
}
