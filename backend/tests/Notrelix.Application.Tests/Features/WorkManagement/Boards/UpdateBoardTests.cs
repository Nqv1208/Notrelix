using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class UpdateBoardTests : WorkManagementHandlerTestBase
{
    private readonly UpdateBoardCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;

    public UpdateBoardTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UpdateBoardCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_RenameBoard_Succeeds()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, "Updated Title", null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardCommand(Guid.CreateVersion7(), "Title", null, null, null, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UpdateDescription_Succeeds()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, null, "New description", null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChangeVisibility_Succeeds()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, null, null, null, BoardVisibility.PublicLink, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdateBackground_Succeeds()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, null, null, "{\"type\":\"image\",\"value\":\"url\"}", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ArchivedBoard_ThrowsBusinessRuleException()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, "New Title", null, null, null, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_AllFieldsNull_NoChanges()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardCommand(board.Id, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
