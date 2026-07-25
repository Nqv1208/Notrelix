using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UpdateBoardItemStatusTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateBoardItemStatusCommandHandler _handler;

    public UpdateBoardItemStatusTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateBoardItemStatusCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesStatus()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var statusField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        statusField.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(statusField);

        var command = new UpdateBoardItemStatusCommand(item.Id, statusField.Options.First().Id.ToString());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardItemStatusCommand(Guid.CreateVersion7(), "Done");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoStatusField_ReturnsFailure()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var textField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Title",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(textField);

        var command = new UpdateBoardItemStatusCommand(item.Id, "Done");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeletedStatusField_ReturnsFailure()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var statusField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        statusField.SoftDelete(TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(statusField);

        var command = new UpdateBoardItemStatusCommand(item.Id, "Done");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
