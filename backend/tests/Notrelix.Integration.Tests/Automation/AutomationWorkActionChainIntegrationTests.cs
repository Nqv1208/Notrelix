using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-XPK-001/002/003 — the flagship Automation→Work target-mutation chain:
/// the Automation work-action port routes through the pure ACL and the
/// WorkManagement Public move action (one producer use case), mutates Work
/// state under the target's authority, and target business rejection is a
/// producer business failure — not a transport retry and not an Automation
/// mutation of Work persistence.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationWorkActionChainIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationWorkActionChainIntegrationTests(PostgresTestContainer db)
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

    private static ICurrentTenantContext WorkspaceTenant(Guid accountId, Guid workspaceId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, userId);
        return tenant;
    }

    private sealed record ChainGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid ItemId,
        Guid BoardId,
        Guid SourceGroupId,
        Guid TargetGroupId,
        Guid ExecutorUserId);

    private async Task<ChainGraph> SeedChainAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var executorUser = User.Create($"xpk-{Guid.NewGuid():N}@example.com", "XPK Executor", "hashed", Now, true);
        var workspace = Workspace.Create(accountId, ownerId, "XPK Workspace", $"xpk-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, executorUser.Id, WorkspaceRole.Member, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var sourceGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Domain.SharedKernel.Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var targetGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Done", Domain.SharedKernel.Color.Create("#00FF00"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, sourceGroup.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(executorUser);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(sourceGroup);
        seed.BoardGroups.Add(targetGroup);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        return new ChainGraph(accountId, workspace.Id, item.Id, board.Id, sourceGroup.Id, targetGroup.Id, executorUser.Id);
    }

    private (IWorkActionPort Port, ApplicationDbContext WorkContext) CreatePort(ChainGraph graph)
    {
        // The runtime composition resolves the adapter against the tenant
        // context of the automation execution's workspace. The delivery
        // pipeline commits the scoped context after the consumer returns.
        var tenant = WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.ExecutorUserId);
        var workContext = _db.CreateContext(tenant);
        var clockMock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);
        var workItemActions = new Application.Features.WorkManagement.BoardItems.Services.MoveBoardItemUseCase(
            workContext, clockMock.Object);
        var actions = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActions(workItemActions);
        return (new WorkItemActionAdapter(actions), workContext);
    }

    [Fact]
    public async Task AutomationMoveItem_MutatesWorkThroughTargetAuthority()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var executionId = Guid.CreateVersion7();

        var result = await port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);

        result.ItemId.Should().Be(graph.ItemId);
        result.GroupId.Should().Be(graph.TargetGroupId);

        // Delivery pipeline commit (dedup filter owns the transaction).
        await workContext.SaveChangesAsync();

        // Verify through a fresh producer-owned read.
        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.TargetGroupId);
    }

    [Fact]
    public async Task AutomationMoveItem_WithInvalidTargetGroup_IsBusinessFailureNotRetry()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var unrelatedGroup = Guid.CreateVersion7();

        var act = () => port.MoveItemAsync(
            graph.ItemId, unrelatedGroup, Guid.CreateVersion7(),
            new AutomationPrincipal(graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);

        // The Work producer rejects the mutation as a business failure; the
        // Automation process records it as terminal business rejection — the
        // delivery mechanism must not treat it as transport retry.
        await act.Should().ThrowAsync<Application.Common.Exceptions.NotFoundException>();

        await workContext.SaveChangesAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.SourceGroupId, "the rejected mutation must not change Work state");
    }
}
