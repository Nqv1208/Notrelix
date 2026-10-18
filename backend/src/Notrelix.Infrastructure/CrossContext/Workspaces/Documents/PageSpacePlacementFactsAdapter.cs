using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.SpacePlacement;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.Documents;

/// <summary>
/// Documents-owned implementation of the published Page placement facts
/// consumed by Workspaces (Space archive/delete, WSP-022). Reads only Documents
/// persistence through the Documents port; Workspaces never touches it.
/// </summary>
public sealed class PageSpacePlacementFactsAdapter : IPageSpacePlacementFacts
{
    private readonly IDocumentDbContext _context;

    public PageSpacePlacementFactsAdapter(IDocumentDbContext context)
    {
        _context = context;
    }

    public async Task<SpacePlacedPageCount> CountInSpaceAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var counts = await _context.Pages
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId && p.SpaceId == spaceId)
            .GroupBy(_ => 1)
            .Select(g => new { Active = g.Count(p => p.Status != PageStatus.Archived), Existing = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? new SpacePlacedPageCount(0, 0) : new SpacePlacedPageCount(counts.Active, counts.Existing);
    }
}
