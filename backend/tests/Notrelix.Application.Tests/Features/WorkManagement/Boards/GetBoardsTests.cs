using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class GetBoardsTests : WorkManagementHandlerTestBase
{
    private readonly GetBoardsQueryHandler _handler;

    public GetBoardsTests()
    {
        _handler = new GetBoardsQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_BoardsExist_ReturnsNonArchivedBoards()
    {
        var board1 = CreateBoard();
        var board2 = CreateBoard();
        SetupBoards(board1, board2);

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

        var query = new GetBoardsQuery(TestWorkspaceId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().BeEmpty();
    }
}
