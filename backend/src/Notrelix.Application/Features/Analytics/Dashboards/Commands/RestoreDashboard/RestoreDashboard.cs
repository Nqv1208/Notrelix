using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RestoreDashboard;

/// <summary>
/// Restores an archived dashboard the caller owns to Active. The product text names no restore window, so any
/// archived dashboard may be restored (Owner question). Restoring an active dashboard is a no-op.
/// </summary>
public record RestoreDashboardCommand(Guid WorkspaceId, Guid DashboardId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class RestoreDashboardCommandHandler : IRequestHandler<RestoreDashboardCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreDashboardCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreDashboardCommand request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, userId)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        dashboard.Restore(userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
