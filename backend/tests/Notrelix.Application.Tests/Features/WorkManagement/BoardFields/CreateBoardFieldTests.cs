using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class CreateBoardFieldTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardFieldCommandHandler _handler;

    public CreateBoardFieldTests()
    {
        _handler = new CreateBoardFieldCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_BoardExists_CreatesField()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardFieldCommand(board.Id, "Status", "Status", null, "a0");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBoardFieldCommand(Guid.CreateVersion7(), "Status", "Status", null, "a0");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BoardArchived_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var command = new CreateBoardFieldCommand(board.Id, "Status", "Status", null, "a0");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TextField_CreatesTextField()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardFieldCommand(board.Id, "Description", "Text", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithSettings_CreatesFieldWithSettings()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardFieldCommand(board.Id, "Priority", "Select", "{\"options\":[]}", "a0");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
