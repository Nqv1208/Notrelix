using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel.Ordering;
using BoardGroup = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// The board items read carries how many live, not-completed Items an Item is blocked by, so the
/// Kanban card can show a "blocked" badge without one link read per card.
/// </summary>
public class GetFullBoardOpenBlockerTests : WorkManagementHandlerTestBase
{
    private readonly GetFullBoardQueryHandler _handler;

    public GetFullBoardOpenBlockerTests()
    {
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var collaboration = new Mock<IWorkManagementCollaborationReadPort>();
        collaboration
            .Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>());
        _handler = new GetFullBoardQueryHandler(DbContextMock.Object, actors.Object, new Notrelix.Application.Features.WorkManagement.BoardItems.Services.BoardItemSummaryBuilder(DbContextMock.Object, actors.Object, collaboration.Object, RequestContextMock.Object), new Moq.Mock<Notrelix.Application.Features.WorkManagement.BoardItems.Services.IBoardItemPageReader>().Object, new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(), RequestContextMock.Object);
    }

    private BoardItemLink Link(Guid source, Guid target, BoardItemLinkType type) => BoardItemLink.Create(
        TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), source,
        ResourceRef.Create(ResourceKind.Create("work-management.board-item"), target, TestWorkspaceId),
        type, TestUserId, TestNow);

    [Fact]
    public async Task Projection_CountsOnlyOpenDependencyBlockers()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Todo", Color.Create("#808080"),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        var blocked = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        var open = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        var done = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        var free = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        done.Complete(TestNow, TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(blocked, open, done, free);
        SetupBoardItemLinks(
            Link(blocked.Id, open.Id, BoardItemLinkType.Dependency),
            Link(blocked.Id, done.Id, BoardItemLinkType.Dependency),
            Link(free.Id, open.Id, BoardItemLinkType.Reference));

        var result = await _handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);

        var cards = result.Data!.Lists.Single().BoardItems;
        cards.Single(c => c.Id == blocked.Id).OpenBlockerCount.Should().Be(1, "the completed blocker no longer blocks");
        cards.Single(c => c.Id == free.Id).OpenBlockerCount.Should().Be(0, "a reference link does not block");
        cards.Single(c => c.Id == open.Id).OpenBlockerCount.Should().Be(0);
    }
}
