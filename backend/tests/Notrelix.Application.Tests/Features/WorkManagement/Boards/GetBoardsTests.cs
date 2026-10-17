using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class GetBoardsTests : WorkManagementHandlerTestBase
{
    private readonly FakeResourceVisibility _visibility = new();
    private readonly GetBoardsQueryHandler _handler;

    public GetBoardsTests()
    {
        _handler = new GetBoardsQueryHandler(DbContextMock.Object, _visibility, RequestContextMock.Object);
    }

    [Fact]
    public async Task Handle_BoardsExist_ReturnsNonArchivedBoards()
    {
        var board1 = CreateBoard();
        var board2 = CreateBoard();
        SetupBoards(board1, board2);
        _visibility.Boards.AddRange([board1.Id, board2.Id]);

        var query = new GetBoardsQuery(TestWorkspaceId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ArchivedBoard_ExcludedFromResults()
    {
        var activeBoard = CreateBoard();
        var archivedBoard = CreateBoard();
        archivedBoard.Archive(TestUserId, TestNow);
        SetupBoards(activeBoard, archivedBoard);
        _visibility.Boards.AddRange([activeBoard.Id, archivedBoard.Id]);

        var query = new GetBoardsQuery(TestWorkspaceId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be(activeBoard.Id);
    }

    [Fact]
    public async Task Handle_EmptyWorkspace_ReturnsEmptyList()
    {
        SetupBoards();

        var query = new GetBoardsQuery(TestWorkspaceId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BoardsInDifferentWorkspace_Excluded()
    {
        var board = CreateBoard(workspaceId: Guid.CreateVersion7());
        SetupBoards(board);
        _visibility.Boards.Add(board.Id);

        var query = new GetBoardsQuery(TestWorkspaceId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BoardTheCallerCannotRead_IsNotListed()
    {
        var visible = CreateBoard();
        var hidden = CreateBoard();
        SetupBoards(visible, hidden);
        _visibility.Boards.Add(visible.Id);

        var result = await _handler.Handle(new GetBoardsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data.Should().ContainSingle().Which.Id.Should().Be(visible.Id,
            "the list applies the single-read ViewBoard decision (GOV-029)");
    }
}
