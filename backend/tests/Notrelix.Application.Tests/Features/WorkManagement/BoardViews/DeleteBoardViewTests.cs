using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class DeleteBoardViewTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DeleteBoardViewCommandHandler _handler;

    public DeleteBoardViewTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new DeleteBoardViewCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesBoardView()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View",
            ViewType.Table, BoardViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId, TestNow);
        SetupBoardViews(view);

        var command = new DeleteBoardViewCommand(board.Id, view.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardViewNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBoardViewCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ViewIdMismatch_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View",
            ViewType.Table, BoardViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId, TestNow);
        SetupBoardViews(view);

        var command = new DeleteBoardViewCommand(board.Id, Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
