using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class GetBoardTests : WorkManagementHandlerTestBase
{
    private readonly GetBoardQueryHandler _handler;

    public GetBoardTests()
    {
        _handler = new GetBoardQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_BoardExists_ReturnsBoardDto()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var query = new GetBoardQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(board.Id);
        result.Data!.Title.Should().Be("Test Board");
        result.Data!.WorkspaceId.Should().Be(TestWorkspaceId);
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var query = new GetBoardQuery(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithMembers_ReturnsCorrectMemberCount()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var member = BoardMember.Create(board.Id, TestUserId, BoardRole.Member, TestNow);
        SetupBoardMembers(member);

        var query = new GetBoardQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithGroups_ReturnsCorrectListCount()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "To Do",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoardGroups(group);

        var query = new GetBoardQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.ListCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ArchivedBoard_ReturnsIsArchivedTrue()
    {
        var board = CreateBoard();
        board.Archive(TestUserId, TestNow);
        SetupBoards(board);

        var query = new GetBoardQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsArchived.Should().BeTrue();
    }
}
