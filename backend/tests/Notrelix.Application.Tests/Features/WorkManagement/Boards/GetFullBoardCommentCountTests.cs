using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel.Ordering;
using BoardGroup = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// The board items read projection (the Kanban card and the table row) carries
/// each Item's comment count from the Collaboration read port: one batched read
/// for all Items of the board, zero when the Item has no comments. Deleted
/// comments are left out by the Collaboration summary itself (see
/// WorkManagementCollaborationReadAdapterTests on PostgreSQL).
/// </summary>
public class GetFullBoardCommentCountTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IWorkManagementCollaborationReadPort> _collaboration = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly GetFullBoardQueryHandler _handler;

    public GetFullBoardCommentCountTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handler = new GetFullBoardQueryHandler(
            DbContextMock.Object,
            _actors.Object,
            _collaboration.Object,
            new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(),
            RequestContextMock.Object);
    }

    [Fact]
    public async Task Projection_CarriesCommentCount_ZeroWhenNone_FromOneBatchedRead()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Todo", Color.Create("#808080"),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        var commented = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        var quiet = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(commented, quiet);
        _collaboration
            .Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>
            {
                [commented.Id] = new(3, 1),
                [quiet.Id] = new(0, 0),
            });

        var result = await _handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);

        var cards = result.Data!.Lists.Single().BoardItems;
        cards.Single(c => c.Id == commented.Id).CommentCount.Should().Be(3);
        cards.Single(c => c.Id == quiet.Id).CommentCount.Should().Be(0);
        _collaboration.Verify(c => c.GetCountsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(commented.Id) && ids.Contains(quiet.Id)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Projection_ItemMissingFromCounts_ShowsZero()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Todo", Color.Create("#808080"),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);
        _collaboration
            .Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>());

        var result = await _handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);

        result.Data!.Lists.Single().BoardItems.Single().CommentCount.Should().Be(0);
    }
}
