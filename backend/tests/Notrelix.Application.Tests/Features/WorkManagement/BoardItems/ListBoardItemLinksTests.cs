using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemLinks;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class ListBoardItemLinksTests : WorkManagementHandlerTestBase
{
    private readonly ListBoardItemLinksQueryHandler _handler;

    public ListBoardItemLinksTests()
    {
        _handler = new ListBoardItemLinksQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_LinksExist_ReturnsLinks()
    {
        var item = CreateBoardItem();
        var other = CreateBoardItem(boardId: item.BoardId);
        SetupBoardItems(item, other);
        var link = CreateBoardItemLink(sourceItemId: item.Id, targetItemId: other.Id);
        SetupBoardItemLinks(link);

        var result = await _handler.Handle(new ListBoardItemLinksQuery(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var seen = result.Data!.Links.Should().ContainSingle().Subject;
        seen.Relation.Should().Be("related");
        seen.Item.Id.Should().Be(other.Id);
        seen.Item.Title.Should().Be("Test Item");
    }

    [Fact]
    public async Task Handle_NoLinks_ReturnsEmptyList()
    {
        var query = new ListBoardItemLinksQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Links.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DependencyReadsAsBlockedByFromTheSourceAndBlocksFromTheTarget()
    {
        var waiting = CreateBoardItem();
        var blocker = CreateBoardItem(boardId: waiting.BoardId);
        SetupBoardItems(waiting, blocker);
        var row = BoardItemLink.Create(
            TestAccountId, TestWorkspaceId, waiting.BoardId, waiting.Id,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), blocker.Id, TestWorkspaceId),
            BoardItemLinkType.Dependency, TestUserId, TestNow);
        SetupBoardItemLinks(row);

        var fromWaiting = await _handler.Handle(new ListBoardItemLinksQuery(waiting.Id), CancellationToken.None);
        var fromBlocker = await _handler.Handle(new ListBoardItemLinksQuery(blocker.Id), CancellationToken.None);

        fromWaiting.Data!.Links.Should().ContainSingle().Which.Relation.Should().Be("blocked-by");
        fromBlocker.Data!.Links.Should().ContainSingle().Which.Relation.Should().Be("blocks");
    }
}
