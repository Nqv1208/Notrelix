namespace Notrelix.Application.Common.Behaviors;

public class TenantBootstrapBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantContext _tenant;
    private readonly IWorkspaceAccessResolver _workspaceAccessResolver;
    private readonly IAccountAccessEvaluator _accountAccessEvaluator;
    private readonly ILogger<TenantBootstrapBehavior<TRequest, TResponse>> _logger;

    public TenantBootstrapBehavior(
        ICurrentTenantContext tenant,
        IWorkspaceAccessResolver workspaceAccessResolver,
        IAccountAccessEvaluator accountAccessEvaluator,
        ILogger<TenantBootstrapBehavior<TRequest, TResponse>> logger)
    {
        _tenant = tenant;
        _workspaceAccessResolver = workspaceAccessResolver;
        _accountAccessEvaluator = accountAccessEvaluator;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IWorkspaceRequest workspaceRequest)
        {
            var workspaceId = workspaceRequest.WorkspaceId;
            if (workspaceId == Guid.Empty)
                throw new ForbiddenException("Invalid workspace context.");

            var actorUserId = _tenant.UserId
                ?? throw new UnauthorizedAccessException("Workspace-scoped request requires authenticated user.");

            var snapshot = await _workspaceAccessResolver.ResolveAsync(workspaceId, actorUserId, cancellationToken);

            if (!snapshot.CanAccess)
            {
                // Security audit: potential cross-tenant access attempt
                _logger.LogWarning(
                    "Cross-tenant access denied: UserId={UserId} RequestedWorkspaceId={WorkspaceId} RequestType={RequestType}",
                    actorUserId,
                    workspaceId,
                    typeof(TRequest).Name);

                throw new ForbiddenException("Access to workspace denied.");
            }

            _tenant.SetWorkspace(snapshot.AccountId, snapshot.WorkspaceId, snapshot.ActorUserId);
        }
        else if (request is IAccountRequest accountRequest)
        {
            var accountId = accountRequest.AccountId;
            if (accountId == Guid.Empty)
                throw new ForbiddenException("Invalid account context.");

            var canAccess = await _accountAccessEvaluator.HasAccountAccess(accountId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning(
                    "Cross-tenant account access denied: UserId={UserId} RequestedAccountId={AccountId} RequestType={RequestType}",
                    _tenant.UserId,
                    accountId,
                    typeof(TRequest).Name);

                throw new ForbiddenException("Access to account denied.");
            }

            _tenant.SetAccount(accountId, _tenant.UserId);
        }

        return await next();
    }
}
