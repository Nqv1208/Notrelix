using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RemoveDashboardWidget;

/// <summary>
/// Removes one widget from a dashboard the caller owns. The widget's source is never touched. An unknown widget is
/// refused (Analytics_Dashboard_WidgetNotFound); a retry under the same Idempotency-Key replays the first answer.
/// </summary>
[IdempotencyOperation("analytics.dashboard.remove-widget.v1")]
public record RemoveDashboardWidgetCommand(Guid WorkspaceId, Guid DashboardId, Guid WidgetId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class RemoveDashboardWidgetCommandHandler : IRequestHandler<RemoveDashboardWidgetCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveDashboardWidgetCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveDashboardWidgetCommand request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, userId)
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        dashboard.RemoveWidget(request.WidgetId, userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
