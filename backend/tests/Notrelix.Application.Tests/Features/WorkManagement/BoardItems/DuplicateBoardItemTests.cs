using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class DuplicateBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly DuplicateBoardItemCommandHandler _handler;

    public DuplicateBoardItemTests()
    {
        _handler = new DuplicateBoardItemCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object,
            Placement,
            KeyAllocator);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDuplicate()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoards(board);
        SetupBoardItems(item);

        var command = new DuplicateBoardItemCommand(item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new DuplicateBoardItemCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedItem_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        var command = new DuplicateBoardItemCommand(item.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
