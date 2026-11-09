using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RenameDashboard;

/// <summary>
/// Renames a dashboard the caller owns (1 to 120 characters after trimming). The same title is a no-op with no version
/// bump and no event; an archived dashboard is read only; a stale expected version is refused.
/// </summary>
public record RenameDashboardCommand(Guid WorkspaceId, Guid DashboardId, string Name, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class RenameDashboardCommandHandler : IRequestHandler<RenameDashboardCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenameDashboardCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RenameDashboardCommand request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, userId)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        dashboard.Rename(request.Name, userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
