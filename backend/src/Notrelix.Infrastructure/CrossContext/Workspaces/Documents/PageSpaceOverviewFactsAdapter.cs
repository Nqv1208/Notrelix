using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.SpaceOverview;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.Documents;

/// <summary>Documents-owned implementation of the published root Page facts consumed by the Workspaces Space overview.</summary>
public sealed class PageSpaceOverviewFactsAdapter : IPageSpaceOverviewFacts
{
    private readonly IDocumentDbContext _context;

    public PageSpaceOverviewFactsAdapter(IDocumentDbContext context)
    {
        _context = context;
    }

    public async Task<SpaceRootPages> ReadRootPagesAsync(
        Guid workspaceId,
        Guid spaceId,
        IReadOnlyCollection<Guid> readablePageIds,
        int take,
        CancellationToken cancellationToken)
    {
        var roots = _context.Pages
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId
                && p.SpaceId == spaceId
                && p.ParentId == null
                && p.DeletedAt == null
                && p.Status != PageStatus.Archived
                && readablePageIds.Contains(p.Id));

        var total = await roots.CountAsync(cancellationToken);
        var recent = await roots
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(take)
            .Select(p => new { p.Id, p.Title, p.UpdatedBy, p.CreatedBy, At = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync(cancellationToken);

        return new SpaceRootPages(
            total,
            recent.Select(p => new SpaceRootPage(p.Id, p.Title, p.UpdatedBy ?? p.CreatedBy, p.At)).ToList());
    }
}
