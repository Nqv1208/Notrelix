namespace Notrelix.Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenantContext _tenant;
    private readonly IAuthorizationDecisionStore _authorizationDecisionStore;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICurrentUser currentUser,
        ICurrentTenantContext tenant,
        IAuthorizationDecisionStore authorizationDecisionStore,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _tenant = tenant;
        _authorizationDecisionStore = authorizationDecisionStore;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Rule 1: Anonymous requests skip all auth
        if (request is IAnonymousRequest)
        {
            return await next();
        }

        // Rule 1.5: System-internal requests skip user authentication.
        // These are background/message-triggered requests, never exposed through HTTP endpoints.
        // Architecture tests enforce the API boundary; consumers and hosted services own the request lifecycle.
        if (request is ISystemInternalRequest)
        {
            _logger.LogTrace(
                "System-internal request {RequestType} bypasses user authentication.",
                typeof(TRequest).Name);
            return await next();
        }

        // Rule 2: Non-anonymous, non-system requests require authenticated user
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            _logger.LogWarning("Authentication required for {RequestType}", typeof(TRequest).Name);
            throw new UnauthorizedException("Authentication required.");
        }

        // Rule 3 & 4: Workspace-scoped and resource-scoped requests require tenant context + permission
        if (request is IWorkspaceRequest or IResourceScopedRequest)
        {
            if (_tenant.AccountId is null || _tenant.AccountId == Guid.Empty)
            {
                _logger.LogError(
                    "Security misconfiguration: {Scope} request {RequestType} has no AccountId in tenant context",
                    request is IWorkspaceRequest ? "Workspace" : "Resource",
                    typeof(TRequest).Name);
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} requires workspace context but AccountId is not set in tenant context.");
            }

            if (_tenant.WorkspaceId is null || _tenant.WorkspaceId == Guid.Empty)
            {
                _logger.LogError(
                    "Security misconfiguration: {Scope} request {RequestType} has no WorkspaceId in tenant context",
                    request is IWorkspaceRequest ? "Workspace" : "Resource",
                    typeof(TRequest).Name);
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} requires workspace context but WorkspaceId is not set in tenant context.");
            }

            if (request is not IRequirePermission and not ISystemInternalRequest)
            {
                _logger.LogError(
                    "Security misconfiguration: {Scope} request {RequestType} does not implement IRequirePermission or ISystemInternalRequest",
                    request is IWorkspaceRequest ? "Workspace" : "Resource",
                    typeof(TRequest).Name);
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} is workspace-scoped without IRequirePermission or ISystemInternalRequest. " +
                    "Add IRequirePermission with permission action/resource, or mark as ISystemInternalRequest.");
            }
        }

        // Rule 5: Account-scoped requests (without workspace) require permission or system marker
        if (request is IAccountRequest and not IWorkspaceRequest)
        {
            if (_tenant.AccountId is null || _tenant.AccountId == Guid.Empty)
            {
                _logger.LogError(
                    "Security misconfiguration: Account request {RequestType} has no AccountId in tenant context",
                    typeof(TRequest).Name);
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} is account-scoped but AccountId is not set in tenant context.");
            }

            if (request is not IRequirePermission and not ISystemInternalRequest)
            {
                _logger.LogError(
                    "Security misconfiguration: Account request {RequestType} does not implement IRequirePermission or ISystemInternalRequest",
                    typeof(TRequest).Name);
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} is account-scoped without IRequirePermission or ISystemInternalRequest. " +
                    "Add IRequirePermission with permission action/resource, or mark as ISystemInternalRequest.");
            }
        }

        // Rule 6 & 7: Evaluate permission for IRequirePermission requests
        if (request is IRequirePermission requirePermission)
        {
            var userId = _currentUser.UserId;
            var workspaceId = requirePermission.Resource.WorkspaceId ?? _tenant.WorkspaceId;

            var scope = request switch
            {
                IWorkspaceRequest => Security.PermissionScope.Workspace,
                IAccountRequest => Security.PermissionScope.Account,
                _ => Security.PermissionScope.Resource,
            };

            if (scope is Security.PermissionScope.Workspace or Security.PermissionScope.Resource && workspaceId is null)
            {
                throw new SecurityMisconfigurationException(
                    $"{typeof(TRequest).Name} requires workspace context for permission evaluation " +
                    $"but Resource.WorkspaceId is null.");
            }

            var decision = await _authorizationDecisionStore.EvaluateAsync(
                new PermissionContext(
                    userId,
                    _tenant.RequireAccountId(),
                    workspaceId,
                    requirePermission.Resource.ResourceType,
                    requirePermission.Resource.ResourceId,
                    requirePermission.Action,
                    scope),
                cancellationToken);

            if (!decision.IsAllowed)
            {
                _logger.LogWarning(
                    "Permission denied: UserId={UserId} Action={Action} ResourceType={ResourceType} ResourceId={ResourceId} WorkspaceId={WorkspaceId} Reason={Reason}",
                    userId,
                    requirePermission.Action,
                    requirePermission.Resource.ResourceType,
                    requirePermission.Resource.ResourceId,
                    requirePermission.Resource.WorkspaceId,
                    decision.ReasonCode);

                if (decision.ReasonCode == "resource_not_found")
                {
                    throw new NotFoundException(requirePermission.Resource.ResourceType.ToString(), requirePermission.Resource.ResourceId);
                }
                throw new ForbiddenException("You do not have permission to perform this action.");
            }
        }

        // Rule 8: Handler is called only when all checks pass
        return await next();
    }
}
