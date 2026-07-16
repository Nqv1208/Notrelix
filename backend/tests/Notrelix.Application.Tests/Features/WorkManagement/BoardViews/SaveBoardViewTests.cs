using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using ViewMode = Notrelix.Domain.WorkManagement.ViewMode;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class SaveBoardViewTests : WorkManagementHandlerTestBase
{
    private readonly SaveBoardViewCommandHandler _handler;

    public SaveBoardViewTests()
    {
        _handler = new SaveBoardViewCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_NewView_CreatesBoardView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Table, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new SaveBoardViewCommand(Guid.CreateVersion7(), ViewMode.Kanban, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithFilters_SavesFilters()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var filters = "{\"status\":\"active\"}";

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Table, filters);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_KanbanMode_CreatesKanbanView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Kanban, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ListMode_CreatesTableView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.List, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
