using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Rules;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Pages nest at most <see cref="PageTreeRules.MaxDepth"/> levels (a root page is level 1). A create under a parent
/// at the limit is refused; a move is refused when the deepest page of the moved subtree would pass the limit.
/// </summary>
public sealed class PageNestingDepthTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void TheLimitIsTenLevels_WithTheRootAtLevelOne()
    {
        PageTreeRules.MaxDepth.Should().Be(10);
        var deepestAllowed = () => PageTreeRules.EnsureCanNestUnder(parentDepth: 9);
        deepestAllowed.Should().NotThrow("a child of a level-9 parent is level 10");
        var act = () => PageTreeRules.EnsureCanNestUnder(parentDepth: 10);
        act.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>().Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded);
    }

    [Fact]
    public async Task CreatingUnderAPageAtTheLimit_IsRefused_AndNothingIsAdded()
    {
        var chain = Chain(10);
        var (sut, pages) = CreateSut(chain);

        var act = () => sut.Handle(new CreatePageCommand(WorkspaceId, "Too deep", chain[^1].Id), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded);
        pages.Should().HaveCount(10);
    }

    [Fact]
    public async Task CreatingUnderAPageOneLevelAboveTheLimit_Succeeds_AndTheNewPageIsLevelTen()
    {
        var chain = Chain(9);
        var parentId = chain[^1].Id;
        var (sut, pages) = CreateSut(chain);

        var result = await sut.Handle(new CreatePageCommand(WorkspaceId, "Deepest allowed", parentId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        pages.Should().HaveCount(10);
        pages[^1].Title.Should().Be("Deepest allowed");
        pages[^1].ParentId.Should().Be(parentId);
    }

    [Fact]
    public async Task MovingASubtreeWhoseDeepestPageWouldPassTheLimit_IsRefused()
    {
        // A chain of nine pages, and a separate page P with one child (a subtree of two levels).
        var chain = Chain(9);
        var moved = NewPage("Moved");
        var child = NewPage("Moved child", moved.Id);
        var sut = CreateMoveSut([.. chain, moved, child]);

        var act = () => sut.Handle(new MovePageCommand(moved.Id, chain[^1].Id, moved.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded);
        moved.ParentId.Should().BeNull("a refused move changes nothing");
    }

    [Fact]
    public async Task MovingASubtreeThatFitsExactlyUnderTheLimit_Succeeds()
    {
        var chain = Chain(8);
        var moved = NewPage("Moved");
        var child = NewPage("Moved child", moved.Id);
        var sut = CreateMoveSut([.. chain, moved, child]);

        var result = await sut.Handle(new MovePageCommand(moved.Id, chain[^1].Id, moved.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        moved.ParentId.Should().Be(chain[^1].Id);
    }

    private static List<Page> Chain(int levels)
    {
        var pages = new List<Page>();
        Guid? parent = null;
        for (var level = 1; level <= levels; level++)
        {
            var page = NewPage($"Level {level}", parent);
            pages.Add(page);
            parent = page.Id;
        }
        return pages;
    }

    private static Page NewPage(string title, Guid? parentId = null) =>
        Page.Create(AccountId, WorkspaceId, title, UserId, Now, parentId);

    private static (CreatePageCommandHandler Sut, List<Page> Pages) CreateSut(List<Page> pages)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(pages).Object);
        var placement = new Mock<IPageSpacePlacement>();
        var creatorGrant = new Mock<IResourceCreatorGrant>();
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        var sut = new CreatePageCommandHandler(context.Object, placement.Object, new InMemoryPageSubtreeStore([.. pages]),
            creatorGrant.Object, requestContext, new FixedClock());
        return (sut, pages);
    }

    private static MovePageCommandHandler CreateMoveSut(List<Page> pages)
    {
        var placement = new Mock<IPageSpacePlacement>();
        var subtree = new Mock<IPageSubtreeAuthorizer>();
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return new MovePageCommandHandler(new InMemoryPageSubtreeStore([.. pages]), placement.Object, subtree.Object, requestContext, new FixedClock());
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
