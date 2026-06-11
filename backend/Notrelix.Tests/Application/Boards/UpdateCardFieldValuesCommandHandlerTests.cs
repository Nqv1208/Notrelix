using Microsoft.EntityFrameworkCore;
using Moq;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardFieldValues;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Boards;

public class UpdateCardFieldValuesCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRejectUserWithoutBoardEditPermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var (card, statusColumn) = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var handler = CreateHandler(context, guestId);

        var act = () => handler.Handle(
            new UpdateCardFieldValuesCommand(card.Id, new Dictionary<Guid, object?> { [statusColumn.Id] = "done" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ShouldUseDomainBehaviorWhenUpdatingStatusColumn()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var (card, statusColumn) = await SeedBoardAsync(context, ownerId, memberId, WorkspaceRole.Member);
        var handler = CreateHandler(context, memberId);

        var result = await handler.Handle(
            new UpdateCardFieldValuesCommand(card.Id, new Dictionary<Guid, object?> { [statusColumn.Id] = "done" }),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        card.Status.Should().Be(CardStatus.Done);
        card.DomainEvents.Should().ContainSingle(item => item is CardStatusChangedEvent);
    }

    private static async Task<(Card Card, BoardColumn StatusColumn)> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole)
    {
        var workspace = Workspace.CreateTeam("Workspace", ownerId);
        workspace.AddMember(userId, userRole);
        var board = Board.Create(workspace.Id, ownerId, "Board", null);
        var list = BoardList.Create(board.Id, "Todo", 1024);
        var card = Card.Create(list.Id, board.Id, ownerId, "Task", 1024);
        var statusColumn = BoardColumn.Create(board.Id, "Status", "status", "{}", 1024);

        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        context.BoardLists.Add(list);
        context.Cards.Add(card);
        context.BoardColumns.Add(statusColumn);
        await context.SaveChangesAsync();
        card.ClearDomainEvents();

        return (card, statusColumn);
    }

    private static UpdateCardFieldValuesCommandHandler CreateHandler(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var permissions = new WorkspacePermissionService(context);

        return new UpdateCardFieldValuesCommandHandler(context, currentUser.Object, permissions);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-card-fields-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
