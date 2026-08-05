using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class GetMyBoardItemsTests : WorkManagementHandlerTestBase
{
    private readonly GetMyBoardItemsQueryHandler _handler;

    public GetMyBoardItemsTests()
    {
        _handler = new GetMyBoardItemsQueryHandler(
            DbContextMock.Object,
            RequestContextMock.Object);
    }

    [Fact]
    public async Task Handle_UserHasAssignedItems_ReturnsItems()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);

        var member = CreateBoardItemMember(itemId: item.Id, userId: TestUserId);
        SetupBoardItemMembers(member);

        var command = new GetMyBoardItemsQuery(TestWorkspaceId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task Handle_UserHasNoAssignments_ReturnsEmptyList()
    {
        var command = new GetMyBoardItemsQuery(TestWorkspaceId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeletedItemsAreExcluded_ReturnsEmptyList()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var item = CreateBoardItem(boardId: board.Id);
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        var member = CreateBoardItemMember(itemId: item.Id, userId: TestUserId);
        SetupBoardItemMembers(member);

        var command = new GetMyBoardItemsQuery(TestWorkspaceId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DifferentWorkspace_ReturnsEmptyList()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);

        var member = CreateBoardItemMember(itemId: item.Id, userId: TestUserId);
        SetupBoardItemMembers(member);

        var command = new GetMyBoardItemsQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
