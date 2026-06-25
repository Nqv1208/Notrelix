using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.Permissions;

public class WorkspacePermissionServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task CanManageWorkspaceAsync_ShouldAllowOnlyOwnerOrAdmin()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, adminId, WorkspaceRole.Admin, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, memberId, WorkspaceRole.Member, ownerId, now));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.CanManageWorkspaceAsync(workspace.Id, ownerId)).Should().BeTrue();
        (await service.CanManageWorkspaceAsync(workspace.Id, adminId)).Should().BeTrue();
        (await service.CanManageWorkspaceAsync(workspace.Id, memberId)).Should().BeFalse();
        (await service.CanManageWorkspaceAsync(workspace.Id, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanManageBoardAsync_ShouldAllowWorkspaceAdminsOrBoardAdmins()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var workspaceMemberId = Guid.NewGuid();
        var boardAdminId = Guid.NewGuid();

        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, workspaceMemberId, WorkspaceRole.Member, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, boardAdminId, WorkspaceRole.Member, ownerId, now));

        var board = Board.Create(workspace.Id, ownerId, "Board", null, now);
        context.Boards.Add(board);

        context.BoardMembers.Add(BoardMember.Create(board.Id, boardAdminId, BoardRole.Admin, now));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.CanManageBoardAsync(board.Id, ownerId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, boardAdminId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, workspaceMemberId)).Should().BeFalse();
    }

    [Fact]
    public async Task CanEditBoardAsync_ShouldAllowWorkspaceMembersButRejectGuests()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var guestId = Guid.NewGuid();

        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, memberId, WorkspaceRole.Member, ownerId, now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.Id, guestId, WorkspaceRole.Guest, ownerId, now));

        var board = Board.Create(workspace.Id, ownerId, "Board", null, now);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.CanEditBoardAsync(board.Id, ownerId)).Should().BeTrue();
        (await service.CanEditBoardAsync(board.Id, memberId)).Should().BeTrue();
        (await service.CanEditBoardAsync(board.Id, guestId)).Should().BeFalse();
    }

    private static WorkspacePermissionService CreateService(ApplicationDbContext context)
    {
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var permissionService = new PermissionService(context, clockMock.Object);
        return new WorkspacePermissionService(permissionService, context);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-permissions-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
