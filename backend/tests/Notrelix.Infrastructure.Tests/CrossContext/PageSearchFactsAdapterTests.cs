using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Infrastructure.CrossContext.Workspaces.Documents;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.CrossContext;

/// <summary>
/// The Documents search facts behind the command search (ADR-010) over EF's in-memory provider: titles match
/// without regard to marks or case, and a Page outside the readable ids, archived, deleted or in another
/// Workspace is never returned; a hidden ancestor's title never appears in a location.
/// </summary>
public sealed class PageSearchFactsAdapterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _author = Guid.CreateVersion7();
    private readonly ApplicationDbContext _context = CreateContext();

    public void Dispose() => _context.Dispose();

    private Page AddPage(string title, Guid? parent = null, Guid? space = null, Guid? workspace = null, DateTimeOffset? at = null)
    {
        var page = Page.Create(_account, workspace ?? _workspace, title, _author, at ?? Now, parent, space);
        _context.Pages.Add(page);
        return page;
    }

    private async Task<IReadOnlyList<Notrelix.Application.Features.Documents.Public.PageSearch.PageSearchHit>> Search(
        string text, int limit = 5, params Page[] readable)
    {
        _context.SaveChanges();
        return await new PageSearchFactsAdapter(_context)
            .SearchAsync(_workspace, readable.Select(p => p.Id).ToList(), text, limit, CancellationToken.None);
    }

    [Fact]
    public async Task Search_MatchesTheTitleWithoutMarksOrCase()
    {
        var space = Guid.CreateVersion7();
        var page = AddPage("Quy trình Hóa đơn", space: space);
        AddPage("Meeting notes");

        var hits = await Search("QUY TRINH hoa don", readable: [page]);

        hits.Should().ContainSingle(h => h.Id == page.Id && h.SpaceId == space && h.EditorUserId == _author && h.Ancestors.Count == 0);
    }

    [Fact]
    public async Task Search_NeverReturnsAPageOutsideTheReadableSet_Archived_Deleted_OrOfAnotherWorkspace()
    {
        var readable = AddPage("Budget readable");
        AddPage("Budget hidden");
        var archived = AddPage("Budget archived");
        archived.Archive(_author, Now);
        var deleted = AddPage("Budget deleted");
        deleted.Delete(_author, Now);
        var foreign = AddPage("Budget foreign", workspace: Guid.CreateVersion7());

        var hits = await Search("budget", readable: [readable, archived, deleted, foreign]);

        hits.Should().ContainSingle(h => h.Id == readable.Id);
    }

    [Fact]
    public async Task Search_ALocationNamesOnlyReadableAncestors_AndStopsAtTheFirstHiddenOne()
    {
        var root = AddPage("Company");
        var hiddenMiddle = AddPage("Confidential plans", parent: root.Id);
        var leaf = AddPage("Roadmap 2027", parent: hiddenMiddle.Id);
        var openRoot = AddPage("Handbook");
        var openChild = AddPage("Onboarding", parent: openRoot.Id);
        var openLeaf = AddPage("Roadmap onboarding", parent: openChild.Id);

        var hits = await Search("roadmap", readable: [root, leaf, openRoot, openChild, openLeaf]);

        hits.Single(h => h.Id == leaf.Id).Ancestors.Should().BeEmpty("the parent is not readable, so nothing above it is told");
        hits.Single(h => h.Id == openLeaf.Id).Ancestors.Should().Equal("Handbook", "Onboarding");
    }

    [Fact]
    public async Task Search_NewestChangeFirst_AndCutToTheLimit()
    {
        var pages = Enumerable.Range(0, 4).Select(i => AddPage($"Notes {i}", at: Now.AddDays(i))).ToArray();

        var hits = await Search("notes", limit: 3, readable: pages);

        hits.Select(h => h.Title).Should().Equal("Notes 3", "Notes 2", "Notes 1");
    }

    [Fact]
    public async Task Search_ShortText_AnswersEmpty()
    {
        var page = AddPage("Alpha");

        (await Search("a", readable: [page])).Should().BeEmpty();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        // The system context lifts the tenant filter, so these tests prove the adapter's own Workspace scoping.
        var tenant = new Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext();
        tenant.SetSystem();
        return new SearchTestContext(options, tenant);
    }

    private sealed class SearchTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
