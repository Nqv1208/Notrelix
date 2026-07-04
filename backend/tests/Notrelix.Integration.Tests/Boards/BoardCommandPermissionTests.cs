using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Boards;

[Collection("Database")]
public class BoardCommandPermissionTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardCommandPermissionTests(PostgresTestContainer db)
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
    public async Task AddBoardMember_ShouldRequireBoardManagePermission()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var addedUserId = Guid.NewGuid();
        var board = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest, addedUserId);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, context, context, timeProvider.Object);
        var handler = new AddBoardMemberCommandHandler(
            context,
            CurrentUser(guestId),
            new WorkspacePermissionService(evaluator, context),
            timeProvider.Object,
            Mock.Of<IWorkspaceAccessResolver>());

        var act = () => handler.Handle(new AddBoardMemberCommand(board.Id, addedUserId, BoardRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateBoardField_ShouldRequireBoardEditPermission()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var board = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, context, context, timeProvider.Object);
        var handlerTenant = new FakeCurrentTenantContext();
        handlerTenant.SetAccount(Guid.NewGuid(), guestId);
        var handler = new CreateBoardFieldCommandHandler(
            context,
            CurrentUser(guestId),
            new WorkspacePermissionService(evaluator, context),
            timeProvider.Object,
            handlerTenant);

        var act = () => handler.Handle(
            new CreateBoardFieldCommand(board.Id, "Risk", "select", "{}", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static async Task<Board> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole,
        Guid? addedUserId = null,
        WorkspaceRole addedUserRole = WorkspaceRole.Member)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", now);
        var workspaceMember = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, userId, userRole, ownerId, now);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, now);

        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(workspaceMember);
        if (addedUserId.HasValue)
        {
            context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, addedUserId.Value, addedUserRole, ownerId, now));
        }
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        return board;
    }

    private static ICurrentUser CurrentUser(Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        return currentUser.Object;
    }
}
