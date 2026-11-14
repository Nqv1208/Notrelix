using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;
using Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The page list and the page history are cursor pages (default 50, at most 200): every page is read once, in
/// order, a hidden page never appears, and a bad cursor or limit is refused.
/// </summary>
public sealed class PageListPagingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task ThePageList_ReadsEveryPageOnce_InCursorOrder()
    {
        var pages = Enumerable.Range(0, 120).Select(i => NewPage($"Page {i:000}")).ToList();
        var sut = new GetWorkspacePagesQueryHandler(Context(pages.ToArray()), Visibility(pages), Actor());

        var seen = new List<Guid>();
        string? cursor = null;
        var pageSizes = new List<int>();
        do
        {
            var page = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId, cursor), CancellationToken.None)).Data!;
            pageSizes.Add(page.Items.Length);
            seen.AddRange(page.Items.Select(p => p.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        pageSizes.Should().Equal(50, 50, 20);
        seen.Should().HaveCount(120).And.OnlyHaveUniqueItems();
        seen.Should().BeInAscendingOrder();
        seen.Should().BeEquivalentTo(pages.Select(p => p.Id));
    }

    [Fact]
    public async Task ThePageList_HonoursALimit_AndNeverPagesPastTheMaximum()
    {
        var pages = Enumerable.Range(0, 260).Select(i => NewPage($"Page {i}")).ToList();
        var sut = new GetWorkspacePagesQueryHandler(Context(pages.ToArray()), Visibility(pages), Actor());

        var defaults = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId), CancellationToken.None)).Data!;
        var capped = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId, Limit: 1000), CancellationToken.None)).Data!;
        var small = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId, Limit: 7), CancellationToken.None)).Data!;

        defaults.Items.Should().HaveCount(50);
        capped.Items.Should().HaveCount(200);
        small.Items.Should().HaveCount(7);
    }

    [Fact]
    public async Task ThePageList_NeverShowsAPageTheCallerCannotSee()
    {
        var visible = NewPage("Shared");
        var hidden = NewPage("Private");
        var sut = new GetWorkspacePagesQueryHandler(Context(visible, hidden), Visibility([visible]), Actor());

        var page = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId), CancellationToken.None)).Data!;

        page.Items.Select(p => p.Id).Should().Equal(visible.Id);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task ThePageList_DisclosesAParentTitleOnAnotherPage()
    {
        var parent = NewPage("Alpha root");
        var child = NewPage("Zeta child", parentId: parent.Id);
        var sut = new GetWorkspacePagesQueryHandler(Context(parent, child), Visibility([parent, child]), Actor());

        var second = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId, Limit: 1), CancellationToken.None)).Data!;
        var rest = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId, second.NextCursor), CancellationToken.None)).Data!;
        var childDto = new[] { second, rest }.SelectMany(p => p.Items).Single(p => p.Id == child.Id);

        childDto.ParentTitle.Should().Be("Alpha root");
    }

    [Fact]
    public void ThePageListValidator_RefusesABadCursorAndALimitOutOfRange()
    {
        var validator = new GetWorkspacePagesQueryValidator();

        validator.Validate(new GetWorkspacePagesQuery(WorkspaceId, "not a cursor!")).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspacePagesQuery(WorkspaceId, Limit: 201)).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspacePagesQuery(WorkspaceId, Limit: 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspacePagesQuery(WorkspaceId, Limit: 200)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TheHistory_ReadsNewestFirst_OneCursorPageAtATime()
    {
        var page = NewPage("Plan");
        var versions = Enumerable.Range(1, 3).Select(n => DocumentVersion.Create(
            AccountId, WorkspaceId, page.Id, n, DocumentSnapshot.Create(Notrelix.Domain.SharedKernel.JsonValue.Create(PageVersionSnapshot.Build(page.Title, []))), UserId, Now.AddMinutes(n),
            null, DocumentVersionKind.Automatic)).ToList();
        var sut = new ListPageVersionsQueryHandler(VersionContext(page, versions));

        var first = (await sut.Handle(new ListPageVersionsQuery(page.Id, Limit: 2), CancellationToken.None)).Data!;
        var second = (await sut.Handle(new ListPageVersionsQuery(page.Id, first.NextCursor), CancellationToken.None)).Data!;

        first.Items.Select(v => v.VersionNumber).Should().Equal(3, 2);
        first.NextCursor.Should().NotBeNull();
        second.Items.Select(v => v.VersionNumber).Should().Equal(1);
        second.NextCursor.Should().BeNull();
    }

    [Fact]
    public void TheHistoryValidator_RefusesABadCursor()
    {
        var validator = new ListPageVersionsQueryValidator();

        validator.Validate(new ListPageVersionsQuery(Guid.NewGuid(), "bogus cursor!")).IsValid.Should().BeFalse();
        validator.Validate(new ListPageVersionsQuery(Guid.NewGuid(), Limit: 500)).IsValid.Should().BeFalse();
    }

    private static Page NewPage(string title, Guid? parentId = null) =>
        Page.Create(AccountId, WorkspaceId, title, UserId, Now, parentId);

    private static IDocumentDbContext Context(params Page[] pages)
    {
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(TestDbSet.Create(pages.ToList()).Object);
        return contextMock.Object;
    }

    private static IDocumentDbContext VersionContext(Page page, List<DocumentVersion> versions)
    {
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        contextMock.Setup(c => c.DocumentVersions).Returns(TestDbSet.Create(versions).Object);
        return contextMock.Object;
    }

    private static IResourceVisibility Visibility(IReadOnlyList<Page> visiblePages)
    {
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisiblePageIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visiblePages.Select(p => p.Id).ToList());
        visibility.Setup(v => v.VisibleSpaceIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        return visibility.Object;
    }

    private static ICurrentRequestContext Actor() => new FakeCurrentRequestContext().AsUser(UserId);
}
