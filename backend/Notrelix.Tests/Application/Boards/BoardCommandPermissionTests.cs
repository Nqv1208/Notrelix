using Microsoft.EntityFrameworkCore;
using Moq;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;
using Notrelix.Application.Features.Boards.Commands.Boards.AddBoardMember;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
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
        var handler = new AddBoardMemberCommandHandler(
            context,
            CurrentUser(memberId),
            new WorkspacePermissionService(context));

        var act = () => handler.Handle(new AddBoardMemberCommand(board.Id, addedUserId, "member"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateBoardColumn_ShouldRequireBoardEditPermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var board = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var handler = new CreateBoardColumnCommandHandler(
            context,
            CurrentUser(guestId),
            new WorkspacePermissionService(context));

        var act = () => handler.Handle(
            new CreateBoardColumnCommand(board.Id, "Risk", "select", "{}", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static async Task<Board> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole)
    {
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(userId, userRole);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);

        context.Workspaces.Add(workspace);
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
