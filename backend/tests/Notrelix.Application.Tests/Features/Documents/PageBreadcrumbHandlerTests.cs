using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageBreadcrumb;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The breadcrumb climbs one parent at a time, root first. An ancestor the caller may view is shown with its
/// title and icon; a run of hidden or missing ancestors is one unavailable crumb with no id, title or icon.
/// </summary>
public sealed class PageBreadcrumbHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task APageTwoLevelsDeep_ShowsTheRootThenTheParentThenThePage()
    {
        var root = NewPage("Root");
        var parent = NewPage("Parent", root.Id);
        var page = NewPage("Page", parent.Id);
        var sut = Create([root, parent, page], visible: [root.Id, parent.Id, page.Id]);

        var crumbs = (await sut.Handle(new GetPageBreadcrumbQuery(page.Id), CancellationToken.None)).Data!;

        crumbs.Select(c => c.Title).Should().Equal("Root", "Parent", "Page");
        crumbs.Should().NotContain(c => c.Unavailable);
        crumbs[^1].Id.Should().Be(page.Id);
    }

    [Fact]
    public async Task AHiddenParent_IsOneUnavailableCrumb_WithoutItsTitleIdOrIcon()
    {
        var root = NewPage("Root");
        var parent = NewPage("Secret", root.Id);
        var page = NewPage("Page", parent.Id);
        var sut = Create([root, parent, page], visible: [root.Id, page.Id]);

        var crumbs = (await sut.Handle(new GetPageBreadcrumbQuery(page.Id), CancellationToken.None)).Data!;

        crumbs.Should().HaveCount(3);
        crumbs[0].Title.Should().Be("Root");
        crumbs[1].Unavailable.Should().BeTrue();
        crumbs[1].Id.Should().BeNull();
        crumbs[1].Title.Should().BeNull();
        crumbs[1].Icon.Should().BeNull();
        crumbs[2].Title.Should().Be("Page");
    }

    [Fact]
    public async Task ARunOfHiddenAncestors_CollapsesToOneUnavailableCrumb()
    {
        var root = NewPage("Root");
        var middle = NewPage("Middle", root.Id);
        var inner = NewPage("Inner", middle.Id);
        var page = NewPage("Page", inner.Id);
        var sut = Create([root, middle, inner, page], visible: [root.Id, page.Id]);

        var crumbs = (await sut.Handle(new GetPageBreadcrumbQuery(page.Id), CancellationToken.None)).Data!;

        crumbs.Select(c => c.Unavailable).Should().Equal(false, true, false);
        crumbs.Select(c => c.Title).Should().Equal("Root", null, "Page");
    }

    [Fact]
    public async Task AHiddenRoot_IsTheFirstCrumb_AndTheVisibleChainFollows()
    {
        var root = NewPage("Hidden root");
        var parent = NewPage("Parent", root.Id);
        var page = NewPage("Page", parent.Id);
        var sut = Create([root, parent, page], visible: [parent.Id, page.Id]);

        var crumbs = (await sut.Handle(new GetPageBreadcrumbQuery(page.Id), CancellationToken.None)).Data!;

        crumbs.Select(c => c.Unavailable).Should().Equal(true, false, false);
        crumbs[1].Title.Should().Be("Parent");
    }

    [Fact]
    public async Task AParentRemovedForGood_ShowsAsUnavailable_AndNeverAsNotFound()
    {
        var root = NewPage("Root");
        var gone = NewPage("Gone", root.Id);
        gone.Archive(UserId, Now.AddMinutes(1));
        gone.Delete(UserId, Now.AddMinutes(2));
        var page = NewPage("Page", gone.Id);
        var sut = Create([root, gone, page], visible: [root.Id, page.Id]);

        var crumbs = (await sut.Handle(new GetPageBreadcrumbQuery(page.Id), CancellationToken.None)).Data!;

        // The climb stops at the missing parent: the root above it cannot be reached through the chain.
        crumbs.Should().HaveCount(2);
        crumbs[0].Unavailable.Should().BeTrue();
        crumbs[0].Id.Should().BeNull();
        crumbs[1].Title.Should().Be("Page");
    }

    [Fact]
    public async Task APageThatIsNotInTheWorkspace_IsNotFound()
    {
        var sut = Create([NewPage("Only")], visible: []);

        var act = () => sut.Handle(new GetPageBreadcrumbQuery(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Page NewPage(string title, Guid? parentId = null) =>
        Page.Create(AccountId, WorkspaceId, title, UserId, Now, parentId);

    private static GetPageBreadcrumbQueryHandler Create(IReadOnlyList<Page> pages, IReadOnlyCollection<Guid> visible)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(pages.ToList()).Object);

        var viewAccess = new Mock<IPageViewAccess>();
        viewAccess.Setup(v => v.CanViewAsync(AccountId, WorkspaceId, It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
            .Returns((Guid _, Guid _, Guid pageId, Guid _, CancellationToken _) => Task.FromResult(visible.Contains(pageId)));

        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return new GetPageBreadcrumbQueryHandler(context.Object, viewAccess.Object, requestContext);
    }
}
