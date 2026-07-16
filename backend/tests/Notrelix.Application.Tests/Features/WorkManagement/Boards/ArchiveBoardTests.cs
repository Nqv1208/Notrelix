using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class ArchiveBoardTests : WorkManagementHandlerTestBase
{
    private readonly ArchiveBoardCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;

    public ArchiveBoardTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new ArchiveBoardCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_BoardExists_ArchivesBoard()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new ArchiveBoardCommand(board.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new ArchiveBoardCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BoardAlreadyArchived_DoesNotThrow()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var command = new ArchiveBoardCommand(board.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
