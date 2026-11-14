using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Queries.GetDashboard;

/// <summary>
/// Opens one dashboard with its widgets. Only the creator may open a private dashboard; anyone else, and a dashboard
/// that does not exist, gets the same NotFound. Widget figures are not read here: no widget has a source yet.
/// </summary>
public sealed record GetDashboardQuery(Guid WorkspaceId, Guid DashboardId)
    : IQuery<Result<DashboardDetailDto>>, IReadRequest, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDetailDto>>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetDashboardQueryHandler(IReportingDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<DashboardDetailDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, _requestContext.UserId)
            .AsNoTracking()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        var widgets = dashboard.Widgets
            .OrderBy(w => w.Position.Y)
            .ThenBy(w => w.Position.X)
            .Select(w => new DashboardWidgetDto(
                w.Id,
                w.Title,
                w.Type.ToString(),
                JsonDocument.Parse(w.Config.Value).RootElement.Clone(),
                w.Position.X,
                w.Position.Y,
                w.Position.W,
                w.Position.H))
            .ToList();

        return Result<DashboardDetailDto>.Success(new DashboardDetailDto(
            dashboard.Id,
            dashboard.Name,
            dashboard.Visibility.ToString(),
            dashboard.Status.ToString(),
            dashboard.CreatedBy,
            dashboard.Version,
            dashboard.CreatedAt,
            dashboard.UpdatedAt,
            widgets));
    }
}
