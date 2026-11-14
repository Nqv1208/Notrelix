using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Domain.Analytics.Dashboards;

namespace Notrelix.Application.Features.Analytics.Dashboards.Services;

/// <summary>
/// Dashboards are private to their creator until sharing exists (UC-ANA-M09). The owner is the audit creator
/// (<c>CreatedBy</c>); a dashboard that is not the caller's is answered exactly like a made-up id (NotFound), so a
/// workspace member cannot tell an existing dashboard of someone else from a missing one.
/// </summary>
internal static class DashboardOwnership
{
    public static IQueryable<Dashboard> OwnedBy(IReportingDbContext context, Guid workspaceId, Guid userId) =>
        context.Dashboards.Where(d => d.WorkspaceId == workspaceId && d.CreatedBy == userId);

    public static NotFoundException NotFound(Guid dashboardId) => new(nameof(Dashboard), dashboardId);
}
