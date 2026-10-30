using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Infrastructure.Documents.Pages;

/// <summary>
/// Loads a page subtree breadth-first through the Documents port (one query per
/// tree level, bounded by the subtree's depth). Pages are tracked so the
/// request data session saves the re-placement in one transaction.
/// </summary>
public sealed class PageSubtreeStore : IPageSubtreeStore
{
    private readonly IDocumentDbContext _context;

    public PageSubtreeStore(IDocumentDbContext context)
    {
        _context = context;
    }

    public Task<Page?> FindAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken) =>
        _context.Pages.FirstOrDefaultAsync(p => p.Id == pageId && p.WorkspaceId == workspaceId, cancellationToken);

    public async Task<int> DepthOfAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        var depth = 0;
        Guid? current = pageId;
        while (current is { } id && depth <= PageTreeRules.MaxDepth)
        {
            var row = await _context.Pages.AsNoTracking()
                .Where(p => p.Id == id && p.WorkspaceId == workspaceId)
                .Select(p => new { p.ParentId })
                .FirstOrDefaultAsync(cancellationToken);
            if (row is null) break;
            depth++;
            current = row.ParentId;
        }

        return depth;
    }

    public async Task<IReadOnlyList<Page>> LoadDescendantsAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        var descendants = new List<Page>();
        var seen = new HashSet<Guid> { pageId };
        var frontier = new List<Guid> { pageId };

        while (frontier.Count > 0)
        {
            var parents = frontier;
            var level = await _context.Pages
                .Where(p => p.WorkspaceId == workspaceId && p.ParentId != null && parents.Contains(p.ParentId.Value))
                .ToListAsync(cancellationToken);

            frontier = [];
            foreach (var child in level.Where(child => seen.Add(child.Id)))
            {
                descendants.Add(child);
                frontier.Add(child.Id);
            }
        }

        return descendants;
    }
}
