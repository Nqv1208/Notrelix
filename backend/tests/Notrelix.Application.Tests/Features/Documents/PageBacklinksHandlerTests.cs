using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageBacklinks;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The backlinks of a page list only the referrers the caller may open: an unreadable or trashed referrer is
/// absent without a count, a Space the caller cannot see is not named, and the list is bounded.
/// </summary>
public sealed class PageBacklinksHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task AReferrerTheCallerCannotOpen_IsAbsent_AndNothingTellsItExists()
    {
        var target = NewPage("Target");
        var open = NewPage("Open referrer");
        var secret = NewPage("Salary plan");
        var sut = Create([target, open, secret], mentioning: [open.Id, secret.Id], visible: [target.Id, open.Id]);

        var docs = (await sut.Handle(new GetPageBacklinksQuery(target.Id), CancellationToken.None)).Data!.Docs;

        docs.Select(d => d.Id).Should().Equal(open.Id);
        docs.Select(d => d.Title).Should().NotContain("Salary plan");
    }

    [Fact]
    public async Task ATrashedReferrer_IsAbsent()
    {
        var target = NewPage("Target");
        var live = NewPage("Live");
        var trashed = NewPage("Trashed");
        trashed.Archive(UserId, Now.AddMinutes(1));
        var sut = Create([target, live, trashed], mentioning: [live.Id, trashed.Id], visible: [target.Id, live.Id, trashed.Id]);

        var docs = (await sut.Handle(new GetPageBacklinksQuery(target.Id), CancellationToken.None)).Data!.Docs;

        docs.Select(d => d.Id).Should().Equal(live.Id);
    }

    [Fact]
    public async Task APageNobodyMentions_HasAnEmptyList()
    {
        var target = NewPage("Target");
        var sut = Create([target], mentioning: [], visible: [target.Id]);

        (await sut.Handle(new GetPageBacklinksQuery(target.Id), CancellationToken.None)).Data!.Docs.Should().BeEmpty();
    }

    [Fact]
    public async Task AMissingPage_IsNotFound()
    {
        var sut = Create([NewPage("Only")], mentioning: [], visible: []);

        var act = () => sut.Handle(new GetPageBacklinksQuery(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MoreReferrersThanTheBound_AreCutAtTheBound_ByTitle()
    {
        var target = NewPage("Target");
        var referrers = Enumerable.Range(0, GetPageBacklinksQueryHandler.MaxBacklinks + 25)
            .Select(i => NewPage($"Referrer {i:D4}")).ToList();
        var sut = Create([target, .. referrers], mentioning: referrers.Select(r => r.Id).ToList(),
            visible: [target.Id, .. referrers.Select(r => r.Id)]);

        var docs = (await sut.Handle(new GetPageBacklinksQuery(target.Id), CancellationToken.None)).Data!.Docs;

        docs.Should().HaveCount(GetPageBacklinksQueryHandler.MaxBacklinks);
        docs[0].Title.Should().Be("Referrer 0000");
    }

    private static Page NewPage(string title) => Page.Create(AccountId, WorkspaceId, title, UserId, Now);

    private static GetPageBacklinksQueryHandler Create(IReadOnlyList<Page> pages, IReadOnlyList<Guid> mentioning, IReadOnlyCollection<Guid> visible)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(pages.ToList()).Object);

        var reader = new Mock<IPageBacklinkReader>();
        reader.Setup(r => r.FindMentioningPageIdsAsync(WorkspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mentioning);

        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisiblePageIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visible.ToList());
        visibility.Setup(v => v.VisibleSpaceIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return new GetPageBacklinksQueryHandler(context.Object, reader.Object, visibility.Object, requestContext);
    }
}
