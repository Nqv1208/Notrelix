using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class CreateBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardItemCommandHandler _handler;

    public CreateBoardItemTests()
    {
        _handler = new CreateBoardItemCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesItem()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Test Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);

        var command = new CreateBoardItemCommand(board.Id, group.Id, "New Item", 0.5);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Item");
        result.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBoardItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "New Item", 0.5);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupBelongsToDifferentBoard_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var otherBoard = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, otherBoard.Id, "Other Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board, otherBoard);
        SetupBoardGroups(group);

        var command = new CreateBoardItemCommand(board.Id, group.Id, "New Item", 0.5);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
