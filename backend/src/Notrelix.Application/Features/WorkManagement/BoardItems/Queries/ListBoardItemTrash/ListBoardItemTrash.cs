using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;

/// <summary>The window a deleted item can be restored in.</summary>
public static class BoardItemTrashPolicy
{
    public static readonly TimeSpan RestoreWindow = TimeSpan.FromDays(30);
}

/// <param name="SubItemCount">The sub-items that went to the Trash with the item and come back with it.</param>
public sealed record BoardItemTrashEntryDto(
    Guid Id,
    string Name,
    string? ItemKey,
    Guid GroupId,
    DateTimeOffset DeletedAt,
    Guid? DeletedBy,
    string? DeletedByName,
    int SubItemCount,
    DateTimeOffset RestorableUntil);

/// <summary>One page of the trash: the rows, the cursor of the next page (null on the last) and how many items are in it in all.</summary>
public sealed record BoardItemTrashPageDto(List<BoardItemTrashEntryDto> Items, string? NextCursor, int Total);

/// <summary>
/// The root items deleted from a board in the last 30 days, newest first, one page at a time (50 by default, at most 200).
/// Seeing the trash needs the same permission restoring does (UpdateItem), so people who cannot restore never see what was deleted.
/// </summary>
/// <param name="Search">Keeps the items whose name or key holds the text.</param>
/// <param name="Cursor">The previous page's NextCursor; none reads the first page.</param>
public record ListBoardItemTrashQuery(Guid BoardId, string? Search = null, string? Cursor = null, int? Limit = null)
    : IQuery<BoardItemTrashPageDto>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardItemTrashQueryHandler : IRequestHandler<ListBoardItemTrashQuery, BoardItemTrashPageDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IActorLookupService _actors;

    public ListBoardItemTrashQueryHandler(
        IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, IActorLookupService actors)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _actors = actors;
    }

    public async Task<BoardItemTrashPageDto> Handle(ListBoardItemTrashQuery request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows, and bypassing it drops its tenant predicate too,
        // so the resolved tenant scope is applied here.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var since = _timeProvider.UtcNow - BoardItemTrashPolicy.RestoreWindow;
        var limit = PageCursor.ClampLimit(request.Limit);
        var after = TimeOrderedPageCursor.Decode(request.Cursor);

        var rows = _context.BoardItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.AccountId == accountId && i.WorkspaceId == workspaceId && i.BoardId == request.BoardId
                && i.DeletedAt != null && i.DeletedAt >= since && i.ParentItemId == null);
        var term = request.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            rows = rows.Where(i => i.Name.ToLower().Contains(lowered) || (i.ItemKey != null && i.ItemKey.ToLower().Contains(lowered)));
        }

        var total = await rows.CountAsync(ct);
        if (after is { } cursor)
        {
            rows = rows.Where(i => i.DeletedAt < cursor.At || (i.DeletedAt == cursor.At && i.Id.CompareTo(cursor.Id) < 0));
        }

        var page = await rows
            .OrderByDescending(i => i.DeletedAt).ThenByDescending(i => i.Id)
            .Take(limit + 1)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.ItemKey,
                i.GroupId,
                i.DeletedAt,
                i.DeletedBy,
                SubItems = _context.BoardItems.IgnoreQueryFilters().Count(child =>
                    child.AccountId == accountId && child.WorkspaceId == workspaceId && child.ParentItemId == i.Id && child.DeletedAt == i.DeletedAt),
            })
            .ToListAsync(ct);

        var hasMore = page.Count > limit;
        if (hasMore) page.RemoveAt(limit);
        var names = (await _actors.FindManyAsync(page.Where(r => r.DeletedBy is not null).Select(r => r.DeletedBy!.Value).Distinct().ToList(), ct))
            .ToDictionary(a => a.UserId);
        var items = page
            .Select(r => new BoardItemTrashEntryDto(
                r.Id, r.Name, r.ItemKey, r.GroupId, r.DeletedAt!.Value, r.DeletedBy,
                r.DeletedBy is { } by ? names.GetValueOrDefault(by)?.Name : null, r.SubItems, r.DeletedAt.Value + BoardItemTrashPolicy.RestoreWindow))
            .ToList();
        var next = hasMore && items.Count > 0 ? TimeOrderedPageCursor.Encode(items[^1].DeletedAt, items[^1].Id) : null;
        return new BoardItemTrashPageDto(items, next, total);
    }
}
