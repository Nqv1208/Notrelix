using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Permissions;

[Collection("Database")]
public class WorkspacePermissionServiceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspacePermissionServiceTests(PostgresTestContainer db)
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

    [Fact]
    public async Task CanManageWorkspaceAsync_ShouldAllowOnlyOwner()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", Now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, adminId, WorkspaceRole.Admin, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.CanManageWorkspaceAsync(workspace.Id, ownerId)).Should().BeTrue();
        (await service.CanManageWorkspaceAsync(workspace.Id, adminId)).Should().BeFalse();
        (await service.CanManageWorkspaceAsync(workspace.Id, memberId)).Should().BeFalse();
        (await service.CanManageWorkspaceAsync(workspace.Id, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanManageBoardAsync_ShouldAllowWorkspaceMembersAndBoardAdmins()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var ownerId = Guid.NewGuid();
        var workspaceMemberId = Guid.NewGuid();
        var boardAdminId = Guid.NewGuid();

        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", Now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, workspaceMemberId, WorkspaceRole.Member, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, boardAdminId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now);
        context.Boards.Add(board);

        context.BoardMembers.Add(BoardMember.Create(board.Id, boardAdminId, BoardRole.Admin, Now));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.CanManageBoardAsync(board.Id, ownerId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, boardAdminId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, workspaceMemberId)).Should().BeTrue();
        (await service.CanManageBoardAsync(board.Id, Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanEditBoardAsync_ShouldAllowWorkspaceMembersButRejectGuests()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var guestId = Guid.NewGuid();

        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", Now);
        context.Workspaces.Add(workspace);

        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, guestId, WorkspaceRole.Guest, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now);
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

        var permissionService = new PermissionService(context, context, context, clockMock.Object);
        return new WorkspacePermissionService(permissionService, context);
    }
}
