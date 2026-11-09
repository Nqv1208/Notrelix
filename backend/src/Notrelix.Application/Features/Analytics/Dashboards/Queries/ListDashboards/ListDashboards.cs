using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Application.Features.Analytics.Dashboards.Services;
using Notrelix.Domain.Analytics.Dashboards;

namespace Notrelix.Application.Features.Analytics.Dashboards.Queries.ListDashboards;

/// <summary>
/// Lists the Active dashboards the caller may open in a Workspace: today that is the caller's own (private) dashboards,
/// since sharing does not exist yet. Archived dashboards are not listed (the product text names no archived view).
/// Newest first, as a cursor page (default 50, max 200).
/// </summary>
public sealed record ListDashboardsQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<DashboardSummaryDto>>>, IReadRequest, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class ListDashboardsQueryHandler : IRequestHandler<ListDashboardsQuery, Result<CursorPage<DashboardSummaryDto>>>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public ListDashboardsQueryHandler(IReportingDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<DashboardSummaryDto>>> Handle(ListDashboardsQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? ListDashboardsQuery.DefaultLimit;
        var userId = _requestContext.UserId;

        var active = await _context.Dashboards
            .AsNoTracking()
            .Where(d => d.WorkspaceId == request.WorkspaceId
                        && d.CreatedBy == userId
                        && d.Status == DashboardStatus.Active)
            .ToListAsync(cancellationToken);

        // Newest first, with the id as the tie-break so the order is total and the cursor is exact.
        IEnumerable<Dashboard> ordered = active
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id);

        if (DashboardCursor.TryDecode(request.Cursor, out var after))
        {
            ordered = active
                .Where(d => d.CreatedAt < after.CreatedAt
                            || (d.CreatedAt == after.CreatedAt && d.Id.CompareTo(after.Id) < 0))
                .OrderByDescending(d => d.CreatedAt)
                .ThenByDescending(d => d.Id);
        }

        var rows = ordered.Take(limit + 1).ToList();
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).Select(d => new DashboardSummaryDto(
            d.Id,
            d.Name,
            d.Visibility.ToString(),
            d.Status.ToString(),
            d.CreatedBy,
            d.Version,
            d.CreatedAt,
            d.UpdatedAt)).ToList();

        var nextCursor = hasMore ? DashboardCursor.Encode(rows[limit - 1].CreatedAt, rows[limit - 1].Id) : null;
        return Result<CursorPage<DashboardSummaryDto>>.Success(new CursorPage<DashboardSummaryDto>(page, nextCursor));
    }
}
