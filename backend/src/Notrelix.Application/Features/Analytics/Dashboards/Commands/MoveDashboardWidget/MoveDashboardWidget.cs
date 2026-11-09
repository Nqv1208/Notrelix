using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.MoveDashboardWidget;

/// <summary>
/// Moves or resizes one widget of a dashboard the caller owns. The position is layout only: no board, item or document
/// moves (product text section 28). Coordinates must be non-negative and the size positive; the product text names no
/// grid width, so no upper bound is enforced yet (Owner question). The concurrency authority is the dashboard.
/// </summary>
public record MoveDashboardWidgetCommand(Guid WorkspaceId, Guid DashboardId, Guid WidgetId, int X, int Y, int W, int H, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class MoveDashboardWidgetCommandHandler : IRequestHandler<MoveDashboardWidgetCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MoveDashboardWidgetCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MoveDashboardWidgetCommand request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, userId)
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        dashboard.MoveWidget(
            request.WidgetId,
            WidgetPosition.Create(request.X, request.Y, request.W, request.H),
            userId,
            _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
