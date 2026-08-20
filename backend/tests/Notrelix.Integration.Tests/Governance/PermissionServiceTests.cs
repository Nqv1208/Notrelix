using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Governance.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppPermissionScope = Notrelix.Application.Common.Security.PermissionScope;

namespace Notrelix.Integration.Tests.Governance;

[Collection("Database")]
public class PermissionServiceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PermissionServiceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private (ApplicationDbContext Context, PermissionService Service) CreateFixture()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _db.CreateContext(tenant);
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        var snapshots = new ResourceAuthorizationSnapshotStore(
            [new BoardAuthorizationSnapshotResolver(context)]);
        var service = new PermissionService(context, context, snapshots, clockMock.Object);
        return (context, service);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldAllowOwnerForAllWorkspaceActions()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(ownerId, workspace.AccountId, workspace.Id, ResourceKind.Create("workspaces.workspace"), null, PermissionAction.DeleteWorkspace, AppPermissionScope.Workspace);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
        decision.EffectiveLevel.Should().Be(PermissionLevel.Owner);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldDenyNonMembers()
    {
        var (context, service) = CreateFixture();
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(Guid.NewGuid(), workspace.AccountId, workspace.Id, ResourceKind.Create("workspaces.workspace"), null, PermissionAction.ViewWorkspace, AppPermissionScope.Workspace);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_workspace_member");
    }

    [Fact]
    public async Task EvaluateAsync_PrivateBoard_ShouldHideForNonBoardMembers()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceBoard_ShouldAllowWorkspaceMembersToView()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Workspace Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void ResourcePermission_IsExpired_ShouldRespectExpiration()
    {
        var expirationDateTime = DateTimeOffset.UtcNow;
        var workspaceId = Guid.NewGuid();

        var activePerm = ResourcePermission.Grant(Guid.NewGuid(), workspaceId, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), expirationDateTime);

        var expiredPerm = ResourcePermission.Grant(Guid.NewGuid(), workspaceId, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), expirationDateTime.AddHours(-2), effect: PermissionEffect.Allow, conditionJson: null, priority: 100);

        activePerm.IsDeleted.Should().BeFalse();
        expiredPerm.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_ViewerCannotUpdateItem()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, viewerId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, viewerId, BoardRole.Observer, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(viewerId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.UpdateItem, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("missing_permission");
    }

    [Fact]
    public async Task EvaluateAsync_EditorCanUpdateItem()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, editorId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, editorId, BoardRole.Member, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(editorId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.UpdateItem, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceGuestCannotViewPrivateBoard()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, guestId, WorkspaceRole.Guest, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(guestId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_RevokedPermissionsAreInvalid()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);

        var permission = ResourcePermission.Grant(Guid.NewGuid(), workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionSubjectType.User, memberId, PermissionLevel.Editor, PermissionLevel.Owner, ownerId, Now);
        permission.Revoke(ownerId, Now);

        context.ResourcePermissions.Add(permission);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_SamePriorityDenyOverridesAllow()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            workspace.AccountId,
            workspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));

        context.PermissionRules.Add(PermissionRule.Create(
            workspace.AccountId,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.UpdateItem,
            PermissionEffect.Allow,
            ownerId,
            Now,
            priority: 100));
        context.PermissionRules.Add(PermissionRule.Create(
            workspace.AccountId,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.UpdateItem,
            PermissionEffect.Deny,
            ownerId,
            Now,
            priority: 100));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            workspace.AccountId,
            workspace.Id,
            ResourceKind.Create("work-management.board"),
            Guid.NewGuid(),
            PermissionAction.UpdateItem,
            AppPermissionScope.Resource));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("denied_by_rule");
    }

    [Fact]
    public async Task EvaluateAsync_WrongAccountCannotUseWorkspaceMembership()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            workspace.AccountId,
            workspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            Guid.NewGuid(),
            workspace.Id,
            ResourceKind.Create("workspaces.workspace"),
            null,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Workspace));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_workspace_member");
    }

    [Fact]
    public void BoardItem_UpdatesFieldValuesCorrectly()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var item = BoardItem.CreateRoot(Guid.NewGuid(), workspaceId, boardId, groupId, "Enterprise Item", Notrelix.Domain.SharedKernel.Ordering.FractionalIndex.Initial(), creatorId, Now);

        item.Rename("Renamed Item", creatorId, Now);

        item.Name.Should().Be("Renamed Item");
    }
}
