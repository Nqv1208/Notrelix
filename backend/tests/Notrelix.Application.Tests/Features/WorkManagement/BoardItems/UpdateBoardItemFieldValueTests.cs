using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UpdateBoardItemFieldValueTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateBoardItemFieldValueCommandHandler _handler;

    public UpdateBoardItemFieldValueTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateBoardItemFieldValueCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesFieldValue()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(field);

        var command = new UpdateBoardItemFieldValueCommand(item.Id, field.Id, "new value");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardItemFieldValueCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "value");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new UpdateBoardItemFieldValueCommand(item.Id, Guid.CreateVersion7(), "value");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FieldBelongsToDifferentBoard_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var otherBoard = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, otherBoard.Id, "Status",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoards(board, otherBoard);
        SetupBoardItems(item);
        SetupBoardFields(field);

        var command = new UpdateBoardItemFieldValueCommand(item.Id, field.Id, "value");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
