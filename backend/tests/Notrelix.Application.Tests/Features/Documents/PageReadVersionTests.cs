using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;
using Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;
using Notrelix.Application.Features.Documents.Pages.Queries.SearchPages;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Every Page read a client moves a Page from (single, list, tree, search)
/// exposes the Page aggregate <c>Version</c>, so a versioned write
/// (<c>Documents.Pages.MoveToSpace</c>) can send it back as
/// <c>ExpectedVersion</c>. The version is the aggregate's current one, not a
/// constant, and exposing it changes nothing about Space disclosure (GOV-029).
/// </summary>
public sealed class PageReadVersionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid SpaceId = Guid.CreateVersion7();

    [Fact]
    public async Task GetPage_ExposesTheCurrentAggregateVersion()
    {
        var page = NewPage("Playbook", spaceId: SpaceId);
        page.Rename("Playbook v2", UserId, Now.AddMinutes(1));
        page.Version.Should().Be(2, "the rename advanced the aggregate version");
        var sut = new GetPageQueryHandler(Context(page), Visibility(page), Actor());

        var dto = (await sut.Handle(new GetPageQuery(page.Id), CancellationToken.None)).Data!;

        dto.Version.Should().Be(page.Version);
        dto.SpaceId.Should().Be(SpaceId, "a visible Space is still disclosed");
    }

    [Fact]
    public async Task GetPage_DoesNotDiscloseAHiddenSpace_WhileStillExposingTheVersion()
    {
        var page = NewPage("Audit", spaceId: SpaceId);
        var sut = new GetPageQueryHandler(Context(page), Visibility([page], visibleSpaces: []), Actor());

        var dto = (await sut.Handle(new GetPageQuery(page.Id), CancellationToken.None)).Data!;

        dto.Version.Should().Be(1);
        dto.SpaceId.Should().BeNull("the caller cannot see the Space (GOV-029)");
    }

    [Fact]
    public async Task GetWorkspacePages_ExposesEachPagesVersion()
    {
        var unchanged = NewPage("Alpha");
        var renamed = NewPage("Beta");
        renamed.Rename("Beta 2", UserId, Now.AddMinutes(1));
        var sut = new GetWorkspacePagesQueryHandler(Context(unchanged, renamed), Visibility(unchanged, renamed), Actor());

        var pages = (await sut.Handle(new GetWorkspacePagesQuery(WorkspaceId), CancellationToken.None)).Data!;

        pages.Should().HaveCount(2);
        pages.Single(p => p.Id == unchanged.Id).Version.Should().Be(1);
        pages.Single(p => p.Id == renamed.Id).Version.Should().Be(2);
    }

    [Fact]
    public async Task GetPageTree_ExposesEachItemsVersion()
    {
        var root = NewPage("Root");
        var child = NewPage("Child", parentId: root.Id);
        child.Rename("Child 2", UserId, Now.AddMinutes(1));
        var sut = new GetPageTreeQueryHandler(Context(root, child), Visibility(root, child), Actor());

        var tree = (await sut.Handle(new GetPageTreeQuery(WorkspaceId), CancellationToken.None)).Data!;

        tree.Single(p => p.Id == root.Id).Version.Should().Be(1);
        tree.Single(p => p.Id == child.Id).Version.Should().Be(2);
        tree.Single(p => p.Id == child.Id).ParentId.Should().Be(root.Id);
    }

    [Fact]
    public async Task SearchPages_ExposesTheVersionOfEachHit()
    {
        var page = NewPage("Quarterly plan");
        page.Rename("Quarterly plan 2027", UserId, Now.AddMinutes(1));
        var sut = new SearchPagesQueryHandler(Context(page), Visibility(page), Actor());

        var hits = (await sut.Handle(new SearchPagesQuery(WorkspaceId, "quarterly"), CancellationToken.None)).Data!;

        hits.Should().ContainSingle().Which.Version.Should().Be(2);
    }

    private static Page NewPage(string title, Guid? parentId = null, Guid? spaceId = null) =>
        Page.Create(Guid.CreateVersion7(), WorkspaceId, title, UserId, Now, parentId, spaceId);

    private static IDocumentDbContext Context(params Page[] pages)
    {
        var contextMock = new Mock<IDocumentDbContext>();
        contextMock.Setup(c => c.Pages).Returns(TestDbSet.Create(pages.ToList()).Object);
        return contextMock.Object;
    }

    private static IResourceVisibility Visibility(params Page[] visiblePages) => Visibility(visiblePages, visibleSpaces: [SpaceId]);

    private static IResourceVisibility Visibility(Page[] visiblePages, IReadOnlyList<Guid> visibleSpaces)
    {
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisiblePageIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visiblePages.Select(p => p.Id).ToList());
        visibility.Setup(v => v.VisibleSpaceIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visibleSpaces);
        return visibility.Object;
    }

    private static ICurrentRequestContext Actor() => new FakeCurrentRequestContext().AsUser(UserId);
}
