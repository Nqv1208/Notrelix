using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class UnarchiveBoardTests : WorkManagementHandlerTestBase
{
    private readonly UnarchiveBoardCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;

    public UnarchiveBoardTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UnarchiveBoardCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ArchivedBoard_UnarchivesBoard()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var command = new UnarchiveBoardCommand(board.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new UnarchiveBoardCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BoardNotArchived_DoesNotThrow()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UnarchiveBoardCommand(board.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
