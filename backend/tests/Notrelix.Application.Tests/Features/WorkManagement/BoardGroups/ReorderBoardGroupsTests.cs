using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class ReorderBoardGroupsTests : WorkManagementHandlerTestBase
{
    private readonly ReorderBoardGroupsCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public ReorderBoardGroupsTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new ReorderBoardGroupsCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReorder_UpdatesPositions()
    {
        var board = CreateBoard();
        var group1 = CreateBoardGroup(boardId: board.Id);
        var group2 = CreateBoardGroup(boardId: board.Id);
        SetupBoards(board);
        SetupBoardGroups(group1, group2);

        var items = new List<ReorderItem>
        {
            new(group1.Id, 2.0),
            new(group2.Id, 1.0)
        };
        var command = new ReorderBoardGroupsCommand(board.Id, items);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var items = new List<ReorderItem>
        {
            new(Guid.CreateVersion7(), 1.0)
        };
        var command = new ReorderBoardGroupsCommand(board.Id, items);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupBelongsToDifferentBoard_ThrowsBusinessRuleViolationException()
    {
        var board1 = CreateBoard();
        var board2 = CreateBoard();
        var group1 = CreateBoardGroup(boardId: board1.Id);
        SetupBoards(board1, board2);
        SetupBoardGroups(group1);

        var items = new List<ReorderItem>
        {
            new(group1.Id, 1.0)
        };
        var command = new ReorderBoardGroupsCommand(board2.Id, items);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Handle_EmptyItems_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new ReorderBoardGroupsCommand(board.Id, new List<ReorderItem>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardGroup CreateBoardGroup(Guid? id = null, Guid? boardId = null)
    {
        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Group",
            Color.Create("#808080"),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            group.GetType().GetProperty(nameof(BoardGroup.Id))!.SetValue(group, id.Value);
        return group;
    }
}
