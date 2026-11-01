using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListArchivedBoardItems;

public sealed record ArchivedBoardItemEntryDto(
    Guid Id,
    string Name,
    string? ItemKey,
    Guid GroupId,
    DateTimeOffset ArchivedAt,
    Guid? ArchivedBy,
    long Version);

/// <summary>
/// The root items archived on a board, newest first, at most 200 and optionally narrowed by a name search.
/// Seeing the archive needs the same permission unarchiving does (UpdateItem), so people who cannot bring an
/// item back never see what was archived. Sub-items archived with their parent come back with it, so they are
/// not listed on their own.
/// </summary>
public record ListArchivedBoardItemsQuery(Guid BoardId, string? Search = null) : IQuery<List<ArchivedBoardItemEntryDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public const int MaxRows = 200;

    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListArchivedBoardItemsQueryHandler : IRequestHandler<ListArchivedBoardItemsQuery, List<ArchivedBoardItemEntryDto>>
{
    private readonly IWorkManagementDbContext _context;

    public ListArchivedBoardItemsQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<List<ArchivedBoardItemEntryDto>> Handle(ListArchivedBoardItemsQuery request, CancellationToken ct)
    {
        var rows = _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == request.BoardId && i.DeletedAt == null && i.IsArchived && i.ParentItemId == null);
        var term = request.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            rows = rows.Where(i => i.Name.ToLower().Contains(lowered));
        }

        return await rows
            .OrderByDescending(i => i.ArchivedAt)
            .Take(ListArchivedBoardItemsQuery.MaxRows)
            .Select(i => new ArchivedBoardItemEntryDto(i.Id, i.Name, i.ItemKey, i.GroupId, i.ArchivedAt!.Value, i.ArchivedBy, i.Version))
            .ToListAsync(ct);
    }
}
