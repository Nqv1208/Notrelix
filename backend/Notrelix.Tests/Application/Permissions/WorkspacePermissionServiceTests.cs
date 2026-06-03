using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Permissions;

public class WorkspacePermissionServiceTests
{
    [Fact]
    public async Task CanManageWorkspaceAsync_ShouldAllowOnlyOwnerOrAdmin()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(adminId, WorkspaceRole.Admin);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();
        var service = new WorkspacePermissionService(context);

        (await service.CanManageWorkspaceAsync(workspace.Id, ownerId)).Should().BeTrue();
        (await service.CanManageWorkspaceAsync(workspace.Id, adminId)).Should().BeTrue();
        (await service.CanManageWorkspaceAsync(workspace.Id, memberId)).Should().BeFalse();
        (await service.CanManageWorkspaceAsync(workspace.Id, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanManageBoardAsync_ShouldAllowWorkspaceAdminsOrBoardAdmins()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var workspaceMemberId = Guid.NewGuid();
        var boardAdminId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(workspaceMemberId, WorkspaceRole.Member);
        workspace.AddMember(boardAdminId, WorkspaceRole.Member);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);
        board.AddMember(boardAdminId, BoardRole.Admin);
        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();
        var service = new WorkspacePermissionService(context);

        (await service.CanManageBoardAsync(board.Id, ownerId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, boardAdminId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, workspaceMemberId)).Should().BeFalse();
    }

    [Fact]
    public async Task CanEditBoardAsync_ShouldAllowWorkspaceMembersButRejectGuests()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(memberId, WorkspaceRole.Member);
        workspace.AddMember(guestId, WorkspaceRole.Guest);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);
        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();
        var service = new WorkspacePermissionService(context);

        (await service.CanEditBoardAsync(board.Id, ownerId)).Should().BeTrue();
        (await service.CanEditBoardAsync(board.Id, memberId)).Should().BeTrue();
        (await service.CanEditBoardAsync(board.Id, guestId)).Should().BeFalse();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-permissions-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
