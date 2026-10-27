using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;

/// <summary>The window a deleted item can be restored in.</summary>
public static class BoardItemTrashPolicy
{
    public static readonly TimeSpan RestoreWindow = TimeSpan.FromDays(30);
}

public sealed record BoardItemTrashEntryDto(
    Guid Id,
    string Name,
    string? ItemKey,
    Guid GroupId,
    DateTimeOffset DeletedAt,
    Guid? DeletedBy,
    DateTimeOffset RestorableUntil);

/// <summary>
/// The root items deleted from a board in the last 30 days, newest first. Seeing the trash needs the same
/// permission restoring does (UpdateItem), so people who cannot restore never see what was deleted.
/// </summary>
public record ListBoardItemTrashQuery(Guid BoardId) : IQuery<List<BoardItemTrashEntryDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardItemTrashQueryHandler : IRequestHandler<ListBoardItemTrashQuery, List<BoardItemTrashEntryDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;

    public ListBoardItemTrashQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<List<BoardItemTrashEntryDto>> Handle(ListBoardItemTrashQuery request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows, and bypassing it drops its tenant predicate too,
        // so the resolved tenant scope is applied here.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var since = _timeProvider.UtcNow - BoardItemTrashPolicy.RestoreWindow;

        var rows = await _context.BoardItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.AccountId == accountId && i.WorkspaceId == workspaceId && i.BoardId == request.BoardId
                && i.DeletedAt != null && i.DeletedAt >= since && i.ParentItemId == null)
            .OrderByDescending(i => i.DeletedAt)
            .Take(200)
            .Select(i => new { i.Id, i.Name, i.ItemKey, i.GroupId, i.DeletedAt, i.DeletedBy })
            .ToListAsync(ct);

        return rows
            .Select(r => new BoardItemTrashEntryDto(
                r.Id, r.Name, r.ItemKey, r.GroupId, r.DeletedAt!.Value, r.DeletedBy, r.DeletedAt.Value + BoardItemTrashPolicy.RestoreWindow))
            .ToList();
    }
}
