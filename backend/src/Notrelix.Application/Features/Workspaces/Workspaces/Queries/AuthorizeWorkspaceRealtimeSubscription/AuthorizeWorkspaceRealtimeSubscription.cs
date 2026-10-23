using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.AuthorizeWorkspaceRealtimeSubscription;

/// <summary>
/// Authorizes a realtime subscription to a Workspace stream. It is decided by the
/// same permission, scope and membership gates as <c>GetWorkspaceQuery</c>
/// (ViewWorkspace on the workspace resource); the handler only reports the tenant
/// binding the pipeline resolved.
/// </summary>
public record AuthorizeWorkspaceRealtimeSubscriptionQuery(Guid WorkspaceId)
    : IQuery<Result<RealtimeSubscriptionGrant>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class AuthorizeWorkspaceRealtimeSubscriptionQueryHandler
    : IRequestHandler<AuthorizeWorkspaceRealtimeSubscriptionQuery, Result<RealtimeSubscriptionGrant>>
{
    private readonly IExecutionContextReader _context;

    public AuthorizeWorkspaceRealtimeSubscriptionQueryHandler(IExecutionContextReader context)
    {
        _context = context;
    }

    public Task<Result<RealtimeSubscriptionGrant>> Handle(
        AuthorizeWorkspaceRealtimeSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var snapshot = _context.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved.");
        if (snapshot.AccountId is not { } accountId || snapshot.WorkspaceId is not { } workspaceId)
        {
            throw new SecurityMisconfigurationException("Realtime authorization requires a resolved tenant binding.");
        }

        return Task.FromResult(Result<RealtimeSubscriptionGrant>.Success(
            new RealtimeSubscriptionGrant(accountId, workspaceId)));
    }
}
