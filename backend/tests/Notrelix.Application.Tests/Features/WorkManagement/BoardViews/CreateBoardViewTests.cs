using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class CreateBoardViewTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardViewCommandHandler _handler;

    public CreateBoardViewTests()
    {
        _handler = new CreateBoardViewCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesBoardView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardViewCommand(board.Id, "My View", "Table", "{}");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("My View");
        result.Type.Should().Be("Table");
        result.BoardId.Should().Be(board.Id);
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBoardViewCommand(Guid.CreateVersion7(), "View", "Table", "{}");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidViewMode_ThrowsArgumentException()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardViewCommand(board.Id, "View", "InvalidMode", "{}");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_KanbanView_CreatesWithCorrectType()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardViewCommand(board.Id, "Kanban", "Kanban", "{}");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Type.Should().Be("Kanban");
    }
}
