using System.Text.RegularExpressions;
using Notrelix.Application.Common.Search;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Public.CommandSearch;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the published Board and Item search consumed by the Workspaces
/// command search (ADR-010). Reads only Work persistence through the Work port. Every query is restricted to
/// the Board ids the caller supplies (already filtered by Governance visibility) and to one Workspace, in the
/// same statement that matches, so a row of an unreadable Board is never selected, ranked or counted.
/// </summary>
public sealed class WorkSearchFactsAdapter : IWorkSearchFacts
{
    /// <summary>Matching Items fetched before ranking; the ranked list is then cut to the caller's limit.</summary>
    private const int CandidateLimit = 100;
    private const int SnippetLength = 24;

    private readonly IWorkManagementDbContext _context;

    public WorkSearchFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<WorkSearchResult> SearchAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<Guid> readableBoardIds,
        string text,
        int itemLimit,
        int boardLimit,
        CancellationToken cancellationToken)
    {
        var pattern = SearchTextFolding.ToPattern(text);
        if (pattern.Length == 0 || readableBoardIds.Count == 0)
            return WorkSearchResult.Empty;

        var liveItems = _context.BoardItems
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId
                && i.DeletedAt == null
                && !i.IsArchived
                && readableBoardIds.Contains(i.BoardId));
        var matching = liveItems.Where(i => Regex.IsMatch(i.Name, pattern)
            || (i.ItemKey != null && Regex.IsMatch(i.ItemKey, pattern)));

        var candidates = await matching
            .OrderByDescending(i => _context.BoardItemMembers.Any(m => m.ItemId == i.Id && m.UserId == userId))
            .ThenBy(i => i.DueAt == null)
            .ThenBy(i => i.DueAt)
            .ThenBy(i => i.Id)
            .Select(i => new Candidate(i.Id, i.ParentItemId, i.Name, i.ItemKey, i.BoardId, i.DueAt))
            .Take(CandidateLimit)
            .ToListAsync(cancellationToken);

        var items = await RankItemsAsync(workspaceId, userId, readableBoardIds, candidates, itemLimit, cancellationToken);
        var boards = await FindBoardsAsync(workspaceId, readableBoardIds, matching, pattern, boardLimit, cancellationToken);
        return new WorkSearchResult(items, boards);
    }

    private async Task<IReadOnlyList<WorkSearchItem>> RankItemsAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<Guid> readableBoardIds,
        List<Candidate> candidates,
        int itemLimit,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return [];

        // A sub-item that matched stands for its top-level Item, which is the result.
        var titleHits = candidates.Where(c => c.ParentItemId is null).ToList();
        var titleIds = titleHits.Select(c => c.Id).ToHashSet();
        var childHits = candidates
            .Where(c => c.ParentItemId is { } parent && !titleIds.Contains(parent))
            .GroupBy(c => c.ParentItemId!.Value)
            .ToDictionary(g => g.Key, g => g.First());
        var parentIds = childHits.Keys.ToList();
        var parents = parentIds.Count == 0
            ? new Dictionary<Guid, Candidate>()
            : (await _context.BoardItems
                .AsNoTracking()
                .Where(i => parentIds.Contains(i.Id)
                    && i.WorkspaceId == workspaceId
                    && i.DeletedAt == null
                    && !i.IsArchived
                    && i.ParentItemId == null
                    && readableBoardIds.Contains(i.BoardId))
                .Select(i => new Candidate(i.Id, i.ParentItemId, i.Name, i.ItemKey, i.BoardId, i.DueAt))
                .ToListAsync(cancellationToken)).ToDictionary(p => p.Id);

        var results = titleHits.Select(c => (Item: c, Child: (string?)null))
            .Concat(childHits
                .Where(pair => parents.ContainsKey(pair.Key))
                .Select(pair => (Item: parents[pair.Key], Child: (string?)Snippet(pair.Value.Name))))
            .ToList();
        if (results.Count == 0)
            return [];

        var ids = results.Select(r => r.Item.Id).ToList();
        var mine = (await _context.BoardItemMembers
            .AsNoTracking()
            .Where(m => ids.Contains(m.ItemId) && m.UserId == userId)
            .Select(m => m.ItemId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var ranked = results
            .OrderByDescending(r => mine.Contains(r.Item.Id))
            .ThenBy(r => r.Child is null ? 0 : 1)
            .ThenBy(r => r.Item.DueAt is null)
            .ThenBy(r => r.Item.DueAt)
            .ThenBy(r => r.Item.Id)
            .Take(itemLimit)
            .ToList();
        var shownIds = ranked.Select(r => r.Item.Id).ToList();
        var boardIds = ranked.Select(r => r.Item.BoardId).Distinct().ToList();

        var boardNames = await _context.Boards
            .AsNoTracking()
            .Where(b => boardIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Title })
            .ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);

        // The person in charge is stated only when it is someone other than the caller.
        var otherAssignee = (await _context.BoardItemMembers
                .AsNoTracking()
                .Where(m => shownIds.Contains(m.ItemId) && m.UserId != userId)
                .Select(m => new { m.ItemId, m.UserId, m.AssignedAt })
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.AssignedAt).ThenBy(m => m.UserId).First().UserId);

        var statuses = await ReadStatusesAsync(boardIds, shownIds, cancellationToken);

        return ranked.Select(r =>
        {
            statuses.TryGetValue(r.Item.Id, out var status);
            return new WorkSearchItem(
                r.Item.Id,
                r.Item.ItemKey,
                r.Item.Name,
                r.Item.BoardId,
                boardNames.GetValueOrDefault(r.Item.BoardId, string.Empty),
                otherAssignee.TryGetValue(r.Item.Id, out var assignee) ? assignee : null,
                mine.Contains(r.Item.Id),
                status?.Name,
                status?.Category,
                r.Item.DueAt,
                r.Child);
        }).ToList();
    }

    private async Task<Dictionary<Guid, StatusInfo>> ReadStatusesAsync(
        IReadOnlyCollection<Guid> boardIds,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        var fields = await _context.BoardFields
            .AsNoTracking()
            .Include(f => f.Options)
            .Where(f => boardIds.Contains(f.BoardId) && f.DeletedAt == null && f.Type == FieldType.Status)
            .ToListAsync(cancellationToken);
        var statusFieldByBoard = fields
            .GroupBy(f => f.BoardId)
            .ToDictionary(g => g.Key, g => WorkItemSemanticFields.ResolveStatusField(g)!);
        var fieldIds = statusFieldByBoard.Values.Select(f => f.Id).ToList();
        if (fieldIds.Count == 0)
            return [];

        var values = await _context.BoardItemValues
            .AsNoTracking()
            .Where(v => itemIds.Contains(v.ItemId) && fieldIds.Contains(v.FieldId))
            .Select(v => new { v.ItemId, v.FieldId, v.Value })
            .ToListAsync(cancellationToken);
        var fieldById = statusFieldByBoard.Values.ToDictionary(f => f.Id);

        var statuses = new Dictionary<Guid, StatusInfo>();
        foreach (var value in values)
        {
            var optionId = WorkItemSemanticFields.ReadOptionId(value.Value);
            var option = optionId is null ? null : fieldById[value.FieldId].Options.FirstOrDefault(o => o.Id == optionId);
            if (option is not null)
                statuses[value.ItemId] = new StatusInfo(option.Name, CategoryCode(option.Category));
        }

        return statuses;
    }

    private async Task<IReadOnlyList<WorkSearchBoard>> FindBoardsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readableBoardIds,
        IQueryable<BoardItem> matching,
        string pattern,
        int boardLimit,
        CancellationToken cancellationToken)
    {
        // Matching top-level Items per readable Board; a sub-item counts for its parent.
        var counts = (await matching
            .GroupBy(i => i.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Select(i => i.ParentItemId ?? i.Id).Distinct().Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.BoardId, x => x.Count);
        var manyMatches = counts.Where(pair => pair.Value >= 2).Select(pair => pair.Key).ToList();

        var boards = await _context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId
                && b.DeletedAt == null
                && !b.IsArchived
                && readableBoardIds.Contains(b.Id)
                && (Regex.IsMatch(b.Title, pattern) || manyMatches.Contains(b.Id)))
            .Select(b => new { b.Id, b.Title, b.SpaceId, NameMatch = Regex.IsMatch(b.Title, pattern) })
            .ToListAsync(cancellationToken);

        // A Board named by the text first, then the Boards holding the most matching Items.
        return boards
            .OrderByDescending(b => b.NameMatch)
            .ThenByDescending(b => counts.GetValueOrDefault(b.Id))
            .ThenBy(b => b.Title, StringComparer.CurrentCulture)
            .ThenBy(b => b.Id)
            .Take(boardLimit)
            .Select(b => new WorkSearchBoard(b.Id, b.Title, b.SpaceId, counts.GetValueOrDefault(b.Id)))
            .ToList();
    }

    private static string? Snippet(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length <= SnippetLength)
            return trimmed;
        var space = trimmed.LastIndexOf(' ', SnippetLength);
        return (space > SnippetLength / 3 ? trimmed[..space] : trimmed[..SnippetLength]).TrimEnd();
    }

    private static string CategoryCode(WorkStatusCategory? category) => category switch
    {
        WorkStatusCategory.InProgress => "in_progress",
        WorkStatusCategory.Review => "review",
        WorkStatusCategory.Done => "done",
        _ => "todo",
    };

    private sealed record StatusInfo(string Name, string Category);

    private sealed record Candidate(Guid Id, Guid? ParentItemId, string Name, string? ItemKey, Guid BoardId, DateTimeOffset? DueAt);
}
