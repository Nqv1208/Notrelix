namespace Notrelix.Application.Common.Behaviors;

public class WorkspaceContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantContext _tenant;
    private readonly IWorkspaceAccessResolver _workspaceAccessResolver;

    public WorkspaceContextBehavior(
        ICurrentTenantContext tenant,
        IWorkspaceAccessResolver workspaceAccessResolver)
    {
        _tenant = tenant;
        _workspaceAccessResolver = workspaceAccessResolver;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IWorkspaceRequest workspaceRequest)
        {
            var workspaceId = workspaceRequest.WorkspaceId;

            if (workspaceId == Guid.Empty)
            {
                throw new ForbiddenException("Invalid workspace context.");
            }

            // Workspace-scoped request requires authenticated user
            var actorUserId = _tenant.UserId
                ?? throw new UnauthorizedAccessException("Workspace-scoped request requires authenticated user.");

            // Resolve workspace access from DB — single source of truth for AccountId
            var snapshot = await _workspaceAccessResolver.ResolveAsync(
                workspaceId, actorUserId, cancellationToken);

            if (!snapshot.CanAccess)
            {
                throw new ForbiddenException("Access to workspace denied.");
            }

            // Set tenant context with resolved AccountId
            _tenant.SetWorkspace(snapshot.AccountId, snapshot.WorkspaceId, snapshot.ActorUserId);
        }

        return await next();
    }
}
