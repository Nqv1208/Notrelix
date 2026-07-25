using Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class AddLabelToBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly AddLabelToBoardItemCommandHandler _handler;

    public AddLabelToBoardItemTests()
    {
        _handler = new AddLabelToBoardItemCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsLabelToItem()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        var label = CreateLabel(board.Id);
        SetupLabels(label);

        var command = new AddLabelToBoardItemCommand(item.Id, label.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardItemNotFound_ThrowsNotFoundException()
    {
        var label = CreateLabel();
        SetupLabels(label);

        var command = new AddLabelToBoardItemCommand(Guid.CreateVersion7(), label.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LabelNotFound_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new AddLabelToBoardItemCommand(item.Id, Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LabelAlreadyApplied_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        var label = CreateLabel(board.Id);
        SetupLabels(label);
        var existingLink = BoardItemLabel.Create(
            TestAccountId, TestWorkspaceId, board.Id, item.Id, label.Id, TestUserId, TestNow);
        SetupBoardItemLabels(existingLink);

        var command = new AddLabelToBoardItemCommand(item.Id, label.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private Label CreateLabel(Guid? boardId = null)
    {
        return Label.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Label",
            LabelColor.Create("#FF0000"),
            TestUserId,
            TestNow);
    }
}
