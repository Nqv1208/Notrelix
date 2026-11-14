using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.ArchiveDashboard;

/// <summary>
/// Archives a dashboard the caller owns (soft: its configuration is kept and it becomes read only). Archiving an
/// archived dashboard is a no-op. The expected version is the dashboard version the caller read.
/// </summary>
public record ArchiveDashboardCommand(Guid WorkspaceId, Guid DashboardId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("analytics.dashboard"), DashboardId, WorkspaceId);
}

public class ArchiveDashboardCommandHandler : IRequestHandler<ArchiveDashboardCommand, Result>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveDashboardCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveDashboardCommand request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var dashboard = await DashboardOwnership.OwnedBy(_context, request.WorkspaceId, userId)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken)
            ?? throw DashboardOwnership.NotFound(request.DashboardId);

        dashboard.Archive(userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
