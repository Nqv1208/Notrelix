using Notrelix.Application.Features.Chat.Links.Queries.ResolveLink;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Chat.Links;

/// <summary>UC-CHT-G05: a link preview carries a title only for a reader who can view the resource now; every other reader gets a generic link.</summary>
public class ResolveLinkTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IWorkItemNotificationFacts> _items = new();
    private readonly Mock<IPageViewAccess> _pageAccess = new();
    private readonly Mock<IPageTitleFacts> _pageTitles = new();
    private readonly FakeResourceVisibility _visibility = new();

    public ResolveLinkTests()
    {
        _context.Setup(c => c.UserId).Returns(_userId);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _items.Setup(i => i.FindAsync(_workspaceId, _itemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemNotificationFacts(_boardId, "Ship the beta", false, null));
        _pageAccess.Setup(p => p.CanViewAsync(_accountId, _workspaceId, _pageId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _pageTitles.Setup(t => t.TitlesAsync(_workspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [_pageId] = "Launch plan" });
    }

    private Task<Notrelix.Application.Common.Models.Result<Notrelix.Application.Features.Chat.Links.DTOs.LinkPreviewDto>> Resolve(
        string? url = null, string? kind = null, Guid? id = null) =>
        new ResolveLinkQueryHandler(_context.Object, _items.Object, _visibility, _pageAccess.Object, _pageTitles.Object)
            .Handle(new ResolveLinkQuery(_workspaceId, url, kind, id), CancellationToken.None);

    [Fact]
    public async Task A_board_item_the_reader_can_see_has_its_title()
    {
        _visibility.Boards.Add(_boardId);

        var result = await Resolve(kind: ChatLinkTargets.BoardItemKind, id: _itemId);

        result.Data.Should().BeEquivalentTo(new { Kind = ChatLinkTargets.BoardItemKind, ResourceId = _itemId, Accessible = true, Title = "Ship the beta" });
    }

    [Fact]
    public async Task A_board_item_on_a_board_the_reader_cannot_see_is_a_generic_link_without_title()
    {
        var result = await Resolve(kind: ChatLinkTargets.BoardItemKind, id: _itemId);

        result.Data!.Accessible.Should().BeFalse();
        result.Data.Title.Should().BeNull("a private resource never lends its title to a preview");
        result.Data.ResourceId.Should().Be(_itemId);
    }

    [Fact]
    public async Task A_missing_item_looks_exactly_like_a_hidden_one()
    {
        var gone = Guid.NewGuid();

        var result = await Resolve(kind: ChatLinkTargets.BoardItemKind, id: gone);

        result.Data.Should().Be(new Notrelix.Application.Features.Chat.Links.DTOs.LinkPreviewDto(ChatLinkTargets.BoardItemKind, gone, false, null));
    }

    [Fact]
    public async Task A_page_the_reader_can_view_has_its_title_and_one_they_cannot_view_does_not()
    {
        (await Resolve(kind: ChatLinkTargets.PageKind, id: _pageId)).Data!.Title.Should().Be("Launch plan");

        _pageAccess.Setup(p => p.CanViewAsync(_accountId, _workspaceId, _pageId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var hidden = await Resolve(kind: ChatLinkTargets.PageKind, id: _pageId);
        hidden.Data!.Accessible.Should().BeFalse();
        hidden.Data.Title.Should().BeNull();
        _pageTitles.Verify(t => t.TitlesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once, "the title of a page the reader cannot view is never even read");
    }

    [Fact]
    public async Task A_trashed_page_that_has_no_live_title_is_generic()
    {
        _pageTitles.Setup(t => t.TitlesAsync(_workspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        var result = await Resolve(kind: ChatLinkTargets.PageKind, id: _pageId);

        result.Data!.Accessible.Should().BeFalse();
    }

    [Fact]
    public async Task A_product_url_resolves_like_its_reference()
    {
        _visibility.Boards.Add(_boardId);

        var item = await Resolve(url: $"https://app.notrelix.test/workspaces/{_workspaceId}/boards/{_boardId}?view=table&item={_itemId}");
        var page = await Resolve(url: $"/workspaces/{_workspaceId}/docs/{_pageId}");

        item.Data!.Title.Should().Be("Ship the beta");
        page.Data!.Title.Should().Be("Launch plan");
    }

    [Theory]
    [InlineData("https://elsewhere.test/page")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.test/workspaces/00000000-0000-0000-0000-000000000000/docs/00000000-0000-0000-0000-000000000000")]
    [InlineData("/workspaces/not-a-guid/docs/not-a-guid")]
    public async Task A_url_that_is_not_a_board_item_or_page_of_this_workspace_is_refused(string url)
    {
        var result = await Resolve(url: url);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task A_url_of_another_workspace_or_a_board_without_an_item_is_refused_and_reads_nothing()
    {
        var other = await Resolve(url: $"/workspaces/{Guid.NewGuid()}/docs/{_pageId}");
        var board = await Resolve(url: $"/workspaces/{_workspaceId}/boards/{_boardId}");

        other.Succeeded.Should().BeFalse();
        board.Succeeded.Should().BeFalse();
        _pageAccess.VerifyNoOtherCalls();
        _items.VerifyNoOtherCalls();
    }

    [Fact]
    public void The_validator_wants_exactly_one_form_and_a_bounded_url()
    {
        var validator = new ResolveLinkQueryValidator();

        validator.Validate(new ResolveLinkQuery(_workspaceId, null, null, null)).IsValid.Should().BeFalse("nothing to resolve");
        validator.Validate(new ResolveLinkQuery(_workspaceId, "/x", ChatLinkTargets.PageKind, _pageId)).IsValid.Should().BeFalse("both forms");
        validator.Validate(new ResolveLinkQuery(_workspaceId, null, ChatLinkTargets.PageKind, null)).IsValid.Should().BeFalse("a kind needs an id");
        validator.Validate(new ResolveLinkQuery(_workspaceId, null, "chat.message", _pageId)).IsValid.Should().BeFalse("an unsupported kind");
        validator.Validate(new ResolveLinkQuery(_workspaceId, new string('a', ChatLinkTargets.MaxUrlLength + 1), null, null)).IsValid.Should().BeFalse("a long url");
        validator.Validate(new ResolveLinkQuery(_workspaceId, null, ChatLinkTargets.PageKind, _pageId)).IsValid.Should().BeTrue();
        validator.Validate(new ResolveLinkQuery(_workspaceId, "/workspaces/x", null, null)).IsValid.Should().BeTrue();
    }
}
