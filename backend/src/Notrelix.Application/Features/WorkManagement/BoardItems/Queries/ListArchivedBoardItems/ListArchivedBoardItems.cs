using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListArchivedBoardItems;

/// <param name="SubItemCount">The sub-items that come back with the item when it is unarchived.</param>
public sealed record ArchivedBoardItemEntryDto(
    Guid Id,
    string Name,
    string? ItemKey,
    Guid GroupId,
    DateTimeOffset ArchivedAt,
    Guid? ArchivedBy,
    string? ArchivedByName,
    int SubItemCount,
    long Version);

/// <summary>One page of the archive: the rows, the cursor of the next page (null on the last) and how many items match in all.</summary>
public sealed record ArchivedBoardItemPageDto(List<ArchivedBoardItemEntryDto> Items, string? NextCursor, int Total);

/// <summary>
/// The root items archived on a board, newest first, one page at a time (50 by default, at most 200) and optionally narrowed
/// by a name search. Seeing the archive needs the same permission unarchiving does (UpdateItem), so people who cannot bring
/// an item back never see what was archived. Sub-items archived with their parent come back with it, so they are not listed
/// on their own.
/// </summary>
/// <param name="Cursor">The previous page's NextCursor; none reads the first page.</param>
public record ListArchivedBoardItemsQuery(Guid BoardId, string? Search = null, string? Cursor = null, int? Limit = null)
    : IQuery<ArchivedBoardItemPageDto>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListArchivedBoardItemsQueryHandler : IRequestHandler<ListArchivedBoardItemsQuery, ArchivedBoardItemPageDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actors;

    public ListArchivedBoardItemsQueryHandler(IWorkManagementDbContext context, IActorLookupService actors)
    {
        _context = context;
        _actors = actors;
    }

    public async Task<ArchivedBoardItemPageDto> Handle(ListArchivedBoardItemsQuery request, CancellationToken ct)
    {
        var limit = PageCursor.ClampLimit(request.Limit);
        var after = TimeOrderedPageCursor.Decode(request.Cursor);

        var rows = _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == request.BoardId && i.DeletedAt == null && i.IsArchived && i.ParentItemId == null);
        var term = request.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            rows = rows.Where(i => i.Name.ToLower().Contains(lowered) || (i.ItemKey != null && i.ItemKey.ToLower().Contains(lowered)));
        }

        var total = await rows.CountAsync(ct);
        if (after is { } cursor)
        {
            rows = rows.Where(i => i.ArchivedAt < cursor.At || (i.ArchivedAt == cursor.At && i.Id.CompareTo(cursor.Id) < 0));
        }

        var page = await rows
            .OrderByDescending(i => i.ArchivedAt).ThenByDescending(i => i.Id)
            .Take(limit + 1)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.ItemKey,
                i.GroupId,
                ArchivedAt = i.ArchivedAt!.Value,
                i.ArchivedBy,
                i.Version,
                SubItems = _context.BoardItems.Count(child => child.ParentItemId == i.Id && child.DeletedAt == null),
            })
            .ToListAsync(ct);

        var hasMore = page.Count > limit;
        if (hasMore) page.RemoveAt(limit);
        var names = (await _actors.FindManyAsync(page.Where(r => r.ArchivedBy is not null).Select(r => r.ArchivedBy!.Value).Distinct().ToList(), ct))
            .ToDictionary(a => a.UserId);
        var items = page
            .Select(r => new ArchivedBoardItemEntryDto(
                r.Id, r.Name, r.ItemKey, r.GroupId, r.ArchivedAt, r.ArchivedBy,
                r.ArchivedBy is { } by ? names.GetValueOrDefault(by)?.Name : null, r.SubItems, r.Version))
            .ToList();
        var next = hasMore && items.Count > 0 ? TimeOrderedPageCursor.Encode(items[^1].ArchivedAt, items[^1].Id) : null;
        return new ArchivedBoardItemPageDto(items, next, total);
    }
}
