using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.LinkPageToBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemPageLinks;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class ItemPageLinkTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IResourceLocator> _locator = new();
    private readonly Mock<IPageViewAccess> _access = new();
    private readonly Mock<IPageTitleFacts> _titles = new();
    private static readonly ResourceKind PageKind = ResourceKind.Create("documents.page");

    private BoardItemLink PageLink(BoardItem item, Guid pageId, DateTimeOffset? at = null) =>
        BoardItemLink.Create(
            TestAccountId, TestWorkspaceId, item.BoardId, item.Id,
            ResourceRef.Create(PageKind, pageId, TestWorkspaceId), BoardItemLinkType.Reference, TestUserId, at ?? TestNow);

    private LinkPageToBoardItemCommandHandler Linker() =>
        new(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _locator.Object);

    private void PageExists(Guid pageId) =>
        _locator.Setup(l => l.LocateAsync(It.Is<ResourceRef>(r => r.ResourceId == pageId), TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(PageKind, pageId, TestAccountId, TestWorkspaceId));

    [Fact]
    public async Task Link_SamePageTwice_StaysOneLink()
    {
        var item = CreateBoardItem();
        var page = Guid.CreateVersion7();
        SetupBoardItems(item);
        PageExists(page);

        await Linker().Handle(new LinkPageToBoardItemCommand(item.Id, page), CancellationToken.None);
        var again = await Linker().Handle(new LinkPageToBoardItemCommand(item.Id, page), CancellationToken.None);

        again.Succeeded.Should().BeTrue();
        DbContextMock.Object.BoardItemLinks.Count(l => l.Target.ResourceId == page).Should().Be(1);
    }

    [Fact]
    public async Task Unlink_RemovesOnlyThatPage_NeverItemToItemLinksOrOtherPages()
    {
        var item = CreateBoardItem();
        var kept = Guid.CreateVersion7();
        var removed = Guid.CreateVersion7();
        var itemLink = CreateBoardItemLink(sourceItemId: item.Id);
        SetupBoardItems(item);
        SetupBoardItemLinks(itemLink, PageLink(item, kept), PageLink(item, removed));

        var handler = new UnlinkPageFromBoardItemCommandHandler(DbContextMock.Object, new Mock<ICurrentUser>().Object);
        var result = await handler.Handle(new UnlinkPageFromBoardItemCommand(item.Id, removed), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        DbContextMock.Object.BoardItemLinks.Select(l => l.Id).Should().BeEquivalentTo(
            DbContextMock.Object.BoardItemLinks.Where(l => l.Id == itemLink.Id || l.Target.ResourceId == kept).Select(l => l.Id));
        DbContextMock.Object.BoardItemLinks.Should().HaveCount(2);
        DbContextMock.Object.BoardItemLinks.Should().NotContain(l => l.Target.ResourceId == removed);
    }

    [Fact]
    public async Task Unlink_PageNotLinked_IsANoOp()
    {
        var item = CreateBoardItem();
        var itemLink = CreateBoardItemLink(sourceItemId: item.Id);
        SetupBoardItems(item);
        SetupBoardItemLinks(itemLink);

        var handler = new UnlinkPageFromBoardItemCommandHandler(DbContextMock.Object, new Mock<ICurrentUser>().Object);
        await handler.Handle(new UnlinkPageFromBoardItemCommand(item.Id, Guid.CreateVersion7()), CancellationToken.None);

        DbContextMock.Object.BoardItemLinks.Should().ContainSingle();
    }

    [Fact]
    public async Task List_NamesOnlyPagesTheCallerMaySee_AndCountsTheRest()
    {
        var item = CreateBoardItem();
        var visible = Guid.CreateVersion7();
        var hidden = Guid.CreateVersion7();
        var gone = Guid.CreateVersion7();
        SetupBoardItems(item);
        SetupBoardItemLinks(
            PageLink(item, visible, TestNow.AddMinutes(1)), PageLink(item, hidden, TestNow.AddMinutes(2)), PageLink(item, gone, TestNow.AddMinutes(3)),
            CreateBoardItemLink(sourceItemId: item.Id));
        _access.Setup(a => a.CanViewAsync(TestAccountId, TestWorkspaceId, hidden, TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _access.Setup(a => a.CanViewAsync(TestAccountId, TestWorkspaceId, visible, TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _access.Setup(a => a.CanViewAsync(TestAccountId, TestWorkspaceId, gone, TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _titles.Setup(t => t.TitlesAsync(TestWorkspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [visible] = "Quy trình đối chiếu" });

        var handler = new ListBoardItemPageLinksQueryHandler(DbContextMock.Object, RequestContextMock.Object, _access.Object, _titles.Object);
        var result = await handler.Handle(new ListBoardItemPageLinksQuery(item.Id), CancellationToken.None);

        result.Data!.Pages.Should().ContainSingle().Which.Should().Match<LinkedPageDto>(p => p.PageId == visible && p.Title == "Quy trình đối chiếu");
        result.Data.HiddenCount.Should().Be(1);
        _titles.Verify(t => t.TitlesAsync(TestWorkspaceId, It.Is<IReadOnlyCollection<Guid>>(ids => !ids.Contains(hidden)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_NoPageLinks_IsEmptyWithoutAskingDocuments()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var handler = new ListBoardItemPageLinksQueryHandler(DbContextMock.Object, RequestContextMock.Object, _access.Object, _titles.Object);
        var result = await handler.Handle(new ListBoardItemPageLinksQuery(item.Id), CancellationToken.None);

        result.Data!.Pages.Should().BeEmpty();
        _access.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_UnknownItem_IsNotFound()
    {
        var handler = new ListBoardItemPageLinksQueryHandler(DbContextMock.Object, RequestContextMock.Object, _access.Object, _titles.Object);
        await handler.Invoking(h => h.Handle(new ListBoardItemPageLinksQuery(Guid.CreateVersion7()), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void List_NeedsOnlyToSeeTheItem()
    {
        var query = new ListBoardItemPageLinksQuery(Guid.CreateVersion7());
        query.Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ViewBoard);
        query.Resource.ResourceId.Should().Be(query.BoardItemId);
    }
}
