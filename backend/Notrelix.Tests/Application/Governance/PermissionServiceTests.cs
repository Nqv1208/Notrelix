using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.WorkManagement;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Governance;

public class PermissionServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-perm-tests-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        _permissionService = new PermissionService(_context);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldAllowOwnerForAllWorkspaceActions()
    {
        // Arrange
        var workspace = Workspace.CreateTeam("Test WS", Guid.NewGuid());
        var ownerId = Guid.NewGuid();
        workspace.AddMember(ownerId, WorkspaceRole.Owner);
        _context.Workspaces.Add(workspace);
        _context.SaveChanges();

        var context = new PermissionContext(ownerId, workspace.Id, ResourceType.Workspace, null, PermissionAction.DeleteWorkspace);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeTrue();
        decision.EffectiveLevel.Should().Be(PermissionLevel.Owner);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldDenyNonMembers()
    {
        // Arrange
        var workspace = Workspace.CreateTeam("Test WS", Guid.NewGuid());
        _context.Workspaces.Add(workspace);
        _context.SaveChanges();

        var context = new PermissionContext(Guid.NewGuid(), workspace.Id, ResourceType.Workspace, null, PermissionAction.ViewWorkspace);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_workspace_member");
    }

    [Fact]
    public async Task EvaluateAsync_PrivateBoard_ShouldHideForNonBoardMembers()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Private Board", null, BoardVisibility.Private);
        
        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.SaveChanges();

        var context = new PermissionContext(memberId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.ViewBoard);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found"); // Shielding private boards
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceBoard_ShouldAllowWorkspaceMembersToView()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Workspace Board", null, BoardVisibility.Workspace);
        
        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.SaveChanges();

        var context = new PermissionContext(memberId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.ViewBoard);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void ResourcePermission_IsExpired_ShouldRespectExpiration()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var activePerm = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer);
        
        var expiredPerm = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, expiresAt: now.AddHours(-1));
        
        var futurePerm = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, expiresAt: now.AddHours(1));

        // Assert
        activePerm.IsExpired.Should().BeFalse();
        expiredPerm.IsExpired.Should().BeTrue();
        futurePerm.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void ResourcePermission_CanViewEdit_ShouldRespectLevel()
    {
        // Arrange
        var viewerPerm = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer);
        var editorPerm = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Editor);

        // Assert
        viewerPerm.CanView.Should().BeTrue();
        viewerPerm.CanEdit.Should().BeFalse();
        editorPerm.CanView.Should().BeTrue();
        editorPerm.CanEdit.Should().BeTrue();
    }

    [Fact]
    public void ResourcePermission_UpdateLevel_ShouldChangeLevel()
    {
        // Arrange
        var permission = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer);

        // Act
        permission.UpdateLevel(PermissionLevel.Editor);

        // Assert
        permission.Level.Should().Be(PermissionLevel.Editor);
    }

    [Fact]
    public void ResourcePermission_Revoke_ShouldSetRevoked()
    {
        // Arrange
        var permission = ResourcePermission.Create(Guid.NewGuid(), ResourceType.Board, Guid.NewGuid(), SubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer);
        var revokedBy = Guid.NewGuid();

        // Act
        permission.Revoke(revokedBy);

        // Assert
        permission.IsRevoked.Should().BeTrue();
        permission.RevokedBy.Should().Be(revokedBy);
        permission.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_ViewerCannotUpdateItem()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(viewerId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Board", null, BoardVisibility.Workspace);
        
        // Add viewerId as Observer (Viewer) to the board
        board.AddMember(viewerId, BoardRole.Observer);

        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.SaveChanges();

        var context = new PermissionContext(viewerId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.UpdateItem);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("missing_permission");
    }

    [Fact]
    public async Task EvaluateAsync_EditorCanUpdateItem()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(editorId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Board", null, BoardVisibility.Workspace);
        
        // Add editorId as Member (Editor) to the board
        board.AddMember(editorId, BoardRole.Member);

        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.SaveChanges();

        var context = new PermissionContext(editorId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.UpdateItem);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceGuestCannotViewPrivateBoard()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(guestId, WorkspaceRole.Guest);
        var board = Board.Create(workspace.Id, ownerId, "Private Board", null, BoardVisibility.Private);

        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.SaveChanges();

        var context = new PermissionContext(guestId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.ViewBoard);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_RevokedPermissionsAreInvalid()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Test WS", ownerId);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Private Board", null, BoardVisibility.Private);

        // Add explicit permission but revoke it
        var permission = ResourcePermission.Create(
            workspace.Id,
            ResourceType.Board,
            board.Id,
            SubjectType.User,
            memberId,
            PermissionLevel.Editor,
            ownerId);
        permission.Revoke(ownerId);

        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.ResourcePermissions.Add(permission);
        _context.SaveChanges();

        var context = new PermissionContext(memberId, workspace.Id, ResourceType.Board, board.Id, PermissionAction.ViewBoard);

        // Act
        var decision = await _permissionService.EvaluateAsync(context);

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found"); // Revoked, so not found for private board
    }

    [Fact]
    public void BoardItem_UpdatesFieldValuesCorrectly()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var item = BoardItem.Create(groupId, boardId, workspaceId, creatorId, "Enterprise Item", 1024);

        // Act
        item.ReplaceValues("{\"field1\":\"value1\"}");

        // Assert
        item.ValuesJson.Should().Be("{\"field1\":\"value1\"}");
        var values = item.GetFieldValues();
        values.Should().ContainKey("field1");
        values["field1"].Should().Be("value1");
    }
}
