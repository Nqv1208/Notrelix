using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Domain.Analytics.Dashboards;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;

/// <summary>
/// Creates a private dashboard owned by the caller (any member of the Workspace may create one; the dashboard starts
/// empty and Active). A retry under the same Idempotency-Key returns the dashboard the first attempt created.
/// </summary>
[IdempotencyOperation("analytics.dashboard.create-dashboard.v1")]
public record CreateDashboardCommand(Guid WorkspaceId, string Name)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IIdempotentRequest
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateDashboardCommandHandler : IRequestHandler<CreateDashboardCommand, Result<Guid>>
{
    private readonly IReportingDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDashboardCommandHandler(
        IReportingDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<Result<Guid>> Handle(CreateDashboardCommand request, CancellationToken cancellationToken)
    {
        var dashboard = Dashboard.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            request.Name,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        _context.Dashboards.Add(dashboard);
        return Task.FromResult(Result<Guid>.Success(dashboard.Id));
    }
}
