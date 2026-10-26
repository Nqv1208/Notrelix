using System.Text.RegularExpressions;
using Notrelix.Application.Common.Search;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageSearch;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.Documents;

/// <summary>
/// Documents-owned implementation of the published Page search consumed by the Workspaces command search
/// (ADR-010). Matches Page titles only, inside the readable Page ids the caller supplies and one Workspace, in
/// the same statement; an ancestor title enters a Page's location only when that ancestor is readable too.
/// </summary>
public sealed class PageSearchFactsAdapter : IPageSearchFacts
{
    private const int MaxAncestors = 6;

    private readonly IDocumentDbContext _context;

    public PageSearchFactsAdapter(IDocumentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PageSearchHit>> SearchAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readablePageIds,
        string text,
        int limit,
        CancellationToken cancellationToken)
    {
        var pattern = SearchTextFolding.ToPattern(text);
        if (pattern.Length == 0 || readablePageIds.Count == 0)
            return [];

        var hits = await _context.Pages
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId
                && p.DeletedAt == null
                && p.Status != PageStatus.Archived
                && readablePageIds.Contains(p.Id)
                && Regex.IsMatch(p.Title, pattern))
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(limit)
            .Select(p => new { p.Id, p.Title, p.ParentId, p.SpaceId, p.UpdatedBy, p.CreatedBy, At = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync(cancellationToken);
        if (hits.Count == 0)
            return [];

        // Walk up level by level; the walk of a Page ends at its first ancestor that is not readable.
        var known = new Dictionary<Guid, (string Title, Guid? ParentId)>();
        var next = hits.Where(h => h.ParentId.HasValue).Select(h => h.ParentId!.Value).ToHashSet();
        for (var level = 0; level < MaxAncestors && next.Count > 0; level++)
        {
            var wanted = next.Where(id => !known.ContainsKey(id)).ToList();
            if (wanted.Count > 0)
            {
                var rows = await _context.Pages
                    .AsNoTracking()
                    .Where(p => wanted.Contains(p.Id)
                        && p.WorkspaceId == workspaceId
                        && p.DeletedAt == null
                        && readablePageIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Title, p.ParentId })
                    .ToListAsync(cancellationToken);
                foreach (var row in rows)
                    known[row.Id] = (row.Title, row.ParentId);
            }

            next = wanted.Where(known.ContainsKey).Select(id => known[id].ParentId).OfType<Guid>().ToHashSet();
        }

        return hits.Select(h => new PageSearchHit(
            h.Id,
            h.Title,
            h.SpaceId,
            AncestorsOf(h.ParentId, known),
            h.At,
            h.UpdatedBy ?? h.CreatedBy)).ToList();
    }

    private static IReadOnlyList<string> AncestorsOf(Guid? parentId, IReadOnlyDictionary<Guid, (string Title, Guid? ParentId)> known)
    {
        var path = new List<string>();
        var current = parentId;
        while (current is { } id && path.Count < MaxAncestors && known.TryGetValue(id, out var ancestor))
        {
            path.Insert(0, ancestor.Title);
            current = ancestor.ParentId;
        }

        return path;
    }
}
