using System.Buffers.Binary;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Rules;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Documents.Pages;

/// <summary>
/// Loads a page subtree breadth-first through the Documents port (one query per
/// tree level, bounded by the subtree's depth). Pages are tracked so the
/// request data session saves the re-placement in one transaction.
/// </summary>
public sealed class PageSubtreeStore : IPageSubtreeStore
{
    private readonly ApplicationDbContext _context;

    public PageSubtreeStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LockTreeAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The page tree lock must be acquired inside the active request transaction.");
        }

        var key = ComputeLockKey(workspaceId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>The first 8 bytes of SHA-256 of "documents.page-tree:&lt;workspace-guid-N&gt;", read big-endian.</summary>
    public static long ComputeLockKey(Guid workspaceId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"documents.page-tree:{workspaceId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
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

    public async Task<IReadOnlyDictionary<Guid, Guid?>> LoadAncestryAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        const int WalkLimit = 64;
        var ancestry = new Dictionary<Guid, Guid?>();
        Guid? current = pageId;
        while (current is { } id && ancestry.Count < WalkLimit && !ancestry.ContainsKey(id))
        {
            var row = await _context.Pages.AsNoTracking()
                .Where(p => p.Id == id && p.WorkspaceId == workspaceId && p.DeletedAt == null)
                .Select(p => new { p.ParentId })
                .FirstOrDefaultAsync(cancellationToken);
            if (row is null) break;
            ancestry[id] = row.ParentId;
            current = row.ParentId;
        }

        return ancestry;
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
