namespace Notrelix.Application.Common.Behaviors;

public class TenantBootstrapBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantContext _tenant;
    private readonly ICurrentCredentialContext _credential;
    private readonly ITenantBootstrapStore _tenantBootstrapStore;
    private readonly ILogger<TenantBootstrapBehavior<TRequest, TResponse>> _logger;

    public TenantBootstrapBehavior(
        ICurrentTenantContext tenant,
        ICurrentCredentialContext credential,
        ITenantBootstrapStore tenantBootstrapStore,
        ILogger<TenantBootstrapBehavior<TRequest, TResponse>> logger)
    {
        _tenant = tenant;
        _credential = credential;
        _tenantBootstrapStore = tenantBootstrapStore;
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

            if (_credential.Kind == CredentialKind.ApiToken)
            {
                var boundWorkspaceId = _credential.BoundWorkspaceId;
                if (!boundWorkspaceId.HasValue || boundWorkspaceId.Value != workspaceId)
                {
                    _logger.LogWarning(
                        "API token workspace binding mismatch: ApiTokenId={ApiTokenId} BoundWorkspaceId={BoundWorkspaceId} RequestedWorkspaceId={WorkspaceId} RequestType={RequestType}",
                        _credential.ApiTokenId,
                        boundWorkspaceId,
                        workspaceId,
                        typeof(TRequest).Name);

                    throw new ForbiddenException("API tokens are restricted to their bound workspace.");
                }

                if (!_credential.BoundAccountId.HasValue)
                {
                    _logger.LogWarning(
                        "API token with missing account binding: ApiTokenId={ApiTokenId} RequestType={RequestType}",
                        _credential.ApiTokenId,
                        typeof(TRequest).Name);

                    throw new ForbiddenException("API token account binding is missing.");
                }
            }

            var snapshot = await _tenantBootstrapStore.ResolveWorkspaceAccessAsync(workspaceId, actorUserId, cancellationToken);

            if (_credential.Kind == CredentialKind.ApiToken &&
                snapshot.AccountId != _credential.BoundAccountId)
            {
                _logger.LogWarning(
                    "API token account binding mismatch after membership resolution: ApiTokenId={ApiTokenId} BoundAccountId={BoundAccountId} ResolvedAccountId={ResolvedAccountId} WorkspaceId={WorkspaceId}",
                    _credential.ApiTokenId,
                    _credential.BoundAccountId,
                    snapshot.AccountId,
                    workspaceId);

                throw new ForbiddenException("API token account binding mismatch.");
            }

            if (!snapshot.CanAccess)
            {
                _logger.LogWarning(
                    "Cross-tenant access denied: UserId={UserId} RequestedWorkspaceId={WorkspaceId} RequestType={RequestType}",
                    actorUserId,
                    workspaceId,
                    typeof(TRequest).Name);

                throw new ForbiddenException("Access to workspace denied.");
            }

            if (!snapshot.IsWorkspaceActive)
            {
                _logger.LogWarning(
                    "Request to inactive workspace: UserId={UserId} WorkspaceId={WorkspaceId} RequestType={RequestType}",
                    actorUserId,
                    workspaceId,
                    typeof(TRequest).Name);
            }

            _tenant.SetWorkspace(snapshot.AccountId, snapshot.WorkspaceId, snapshot.ActorUserId);
        }
        else if (request is IAccountRequest)
        {
            if (_credential.Kind == CredentialKind.ApiToken)
            {
                _logger.LogWarning(
                    "API token used for account-scoped request: ApiTokenId={ApiTokenId} RequestType={RequestType}",
                    _credential.ApiTokenId,
                    typeof(TRequest).Name);

                throw new ForbiddenException("API tokens are restricted to their bound workspace.");
            }

            var actorUserId = _tenant.UserId
                ?? throw new UnauthorizedAccessException("Account-scoped request requires authenticated user.");

            var accountId = _tenant.AccountId
                ?? throw new AccountSelectionRequiredException(
                    $"{typeof(TRequest).Name} is account-scoped but no AccountId is selected. " +
                    "Provide account context via route, header, or session.");

            await _tenantBootstrapStore.VerifyAccountAccessAsync(accountId, actorUserId, cancellationToken);

            _logger.LogInformation(
                "Verified account access: UserId={UserId} AccountId={AccountId} RequestType={RequestType}",
                actorUserId,
                accountId,
                typeof(TRequest).Name);

            _tenant.SetAccount(accountId, actorUserId);
        }
        else if (_credential.Kind == CredentialKind.ApiToken && request is IAuthenticatedRequest)
        {
            _logger.LogWarning(
                "API token used for authenticated non-workspace request: ApiTokenId={ApiTokenId} RequestType={RequestType}",
                _credential.ApiTokenId,
                typeof(TRequest).Name);

            throw new ForbiddenException("API tokens are restricted to their bound workspace.");
        }

        return await next();
    }
}
