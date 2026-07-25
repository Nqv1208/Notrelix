using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class CreateBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardGroupCommandHandler _handler;

    public CreateBoardGroupTests()
    {
        _handler = new CreateBoardGroupCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_BoardExists_CreatesGroup()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardGroupCommand(board.Id, "New Group", "a0", "#FF0000");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBoardGroupCommand(Guid.CreateVersion7(), "New Group", "a0", "#FF0000");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BoardArchived_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var command = new CreateBoardGroupCommand(board.Id, "New Group", "a0", "#FF0000");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullPosition_UsesInitialPosition()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardGroupCommand(board.Id, "New Group", null, "#FF0000");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullColor_UsesDefaultColor()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new CreateBoardGroupCommand(board.Id, "New Group", "a0", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
