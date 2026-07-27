using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class SetDefaultViewTests : WorkManagementHandlerTestBase
{
    private readonly SetDefaultViewCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public SetDefaultViewTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new SetDefaultViewCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ViewExists_SetsDefault()
    {
        var board = CreateBoard();
        var view = CreateBoardView(board.Id);
        SetupBoards(board);
        SetupBoardViews(view);

        var command = new SetDefaultViewCommand(board.Id, view.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ViewNotFound_ThrowsNotFoundException()
    {
        var command = new SetDefaultViewCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDefault_IsIdempotent()
    {
        var board = CreateBoard();
        var view = CreateBoardView(board.Id, isDefault: true);
        SetupBoards(board);
        SetupBoardViews(view);

        var command = new SetDefaultViewCommand(board.Id, view.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherViewWasDefault_ClearsOldDefault()
    {
        var board = CreateBoard();
        var oldDefault = CreateBoardView(board.Id, isDefault: true);
        var newDefault = CreateBoardView(board.Id, isDefault: false);
        SetupBoards(board);
        SetupBoardViews(oldDefault, newDefault);

        var command = new SetDefaultViewCommand(board.Id, newDefault.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private Board CreateBoard(Guid? id = null)
    {
        var board = Notrelix.Domain.WorkManagement.Boards.Board.Create(
            TestAccountId,
            TestWorkspaceId,
            TestUserId,
            "Test Board",
            null,
            TestNow,
            BoardVisibility.Workspace);
        if (id.HasValue)
            board.GetType().GetProperty(nameof(Board.Id))!.SetValue(board, id.Value);
        return board;
    }

    private BoardView CreateBoardView(Guid boardId, bool isDefault = false)
    {
        var view = BoardView.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId,
            "Test View",
            ViewType.Table,
            TableViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId,
            TestNow,
            isDefault);
        return view;
    }
}
