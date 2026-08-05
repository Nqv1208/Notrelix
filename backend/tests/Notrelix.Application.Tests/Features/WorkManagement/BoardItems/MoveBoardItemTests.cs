using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class MoveBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly MoveBoardItemCommandHandler _handler;

    public MoveBoardItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new MoveBoardItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_MovesItemToNewGroup()
    {
        var board = CreateBoard();
        var sourceGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Source Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var targetGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Target Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: sourceGroup.Id);
        SetupBoards(board);
        SetupBoardGroups(sourceGroup, targetGroup);
        SetupBoardItems(item);

        var command = new MoveBoardItemCommand(item.Id, targetGroup.Id, 0.75);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.GroupId.Should().Be(targetGroup.Id);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new MoveBoardItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 0.5);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new MoveBoardItemCommand(item.Id, Guid.CreateVersion7(), 0.5);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupBelongsToDifferentBoard_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var otherBoard = CreateBoard();
        var targetGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, otherBoard.Id, "Other Board Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoards(board, otherBoard);
        SetupBoardGroups(targetGroup);
        SetupBoardItems(item);

        var command = new MoveBoardItemCommand(item.Id, targetGroup.Id, 0.5);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
