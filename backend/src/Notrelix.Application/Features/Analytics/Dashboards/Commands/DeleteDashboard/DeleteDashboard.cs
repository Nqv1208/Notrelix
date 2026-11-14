using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.DeleteDashboard;

/// <summary>
/// Permanently deletes a dashboard the caller owns, with its widgets (database cascade). Sources are never touched:
/// a dashboard owns configuration only (product text sections 16, 91 and 92). The client confirms before calling.
/// </summary>
public record DeleteDashboardCommand(Guid WorkspaceId, Guid DashboardId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class DeleteDashboardCommandHandler : IRequestHandler<DeleteDashboardCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public DeleteDashboardCommandHandler(IReportingDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result> Handle(DeleteDashboardCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, _requestContext.UserId)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        _context.Dashboards.Remove(dashboard);
        return Result.Success();
    }
}
