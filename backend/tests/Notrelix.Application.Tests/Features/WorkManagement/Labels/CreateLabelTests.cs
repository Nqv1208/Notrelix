using Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class CreateLabelTests : WorkManagementHandlerTestBase
{
    private readonly CreateLabelCommandHandler _handler;

    public CreateLabelTests()
    {
        _handler = new CreateLabelCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLabel()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateLabelCommand(board.Id, "#FF0000", "Bug");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new CreateLabelCommand(Guid.CreateVersion7(), "#FF0000", "Bug");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidName_CreatesLabel()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateLabelCommand(board.Id, "#00FF00", "Feature");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
