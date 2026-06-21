using Microsoft.EntityFrameworkCore;
using Moq;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Boards;

public class BoardCommandPermissionTests
{
    [Fact]
    public async Task AddBoardMember_ShouldRequireBoardManagePermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var addedUserId = Guid.NewGuid();
        var board = await SeedBoardAsync(context, ownerId, memberId, WorkspaceRole.Member);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, timeProvider.Object);
        var handler = new AddBoardMemberCommandHandler(
            context,
            CurrentUser(memberId),
            new WorkspacePermissionService(evaluator, context),
            timeProvider.Object);

        var act = () => handler.Handle(new AddBoardMemberCommand(board.Id, addedUserId, "member"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateBoardField_ShouldRequireBoardEditPermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var board = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, timeProvider.Object);
        var handler = new CreateBoardFieldCommandHandler(
            context,
            CurrentUser(guestId),
            new WorkspacePermissionService(evaluator, context),
            timeProvider.Object);

        var act = () => handler.Handle(
            new CreateBoardFieldCommand(board.Id, "Risk", "select", "{}", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static async Task<Board> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", now);
        var workspaceMember = WorkspaceMember.Create(workspace.Id, userId, userRole, ownerId, now);
        var board = Board.Create(workspace.Id, ownerId, "Board", null, now);

        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(workspaceMember);
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-board-permissions-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
