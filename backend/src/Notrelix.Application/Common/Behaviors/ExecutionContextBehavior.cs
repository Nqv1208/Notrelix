using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class ExecutionContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IExecutionContextAccessor _executionContext;
    private readonly ICurrentTenantContext _tenant;
    private readonly ICurrentCredentialContext _credential;
    private readonly IResourceLocator _resourceLocator;
    private readonly ITenantBootstrapStore _tenantBootstrapStore;

    public ExecutionContextBehavior(
        IRequestDescriptorRegistry descriptors,
        IExecutionContextAccessor executionContext,
        ICurrentTenantContext tenant,
        ICurrentCredentialContext credential,
        IResourceLocator resourceLocator,
        ITenantBootstrapStore tenantBootstrapStore)
    {
        _descriptors = descriptors;
        _executionContext = executionContext;
        _tenant = tenant;
        _credential = credential;
        _resourceLocator = resourceLocator;
        _tenantBootstrapStore = tenantBootstrapStore;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var stage = PipelineActivitySource.Instance.StartActivity("context.resolve");

        var descriptor = _descriptors.GetRequired(typeof(TRequest));
        var userId = _executionContext.UserId ?? _tenant.UserId;
        Guid? accountId = _tenant.AccountId;
        Guid? workspaceId = null;
        ResourceRef? resource = null;

        switch (descriptor.Scope)
        {
            case ApplicationScopeKind.Workspace:
                {
                    var workspaceRequest = (IWorkspaceRequest)request;
                    if (workspaceRequest.WorkspaceId == Guid.Empty)
                    {
                        throw new ForbiddenException("Invalid workspace context.");
                    }

                    var actorId = RequireUser(userId, "Workspace-scoped request requires authenticated user.");
                    EnforceApiTokenWorkspaceBinding(workspaceRequest.WorkspaceId);

                    // Resolve the tenant binding from the workspace itself and
                    // enforce membership/workspace-state gates exactly as the
                    // legacy bootstrap stage did (characterization parity).
                    var snapshot = await _tenantBootstrapStore.ResolveWorkspaceAccessAsync(
                        workspaceRequest.WorkspaceId, actorId, cancellationToken);

                    if (_credential.Kind == CredentialKind.ApiToken
                        && snapshot.AccountId != _credential.BoundAccountId)
                    {
                        throw new ForbiddenException("API token account binding mismatch.");
                    }

                    if (!snapshot.CanAccess)
                    {
                        throw new ForbiddenException("Access to workspace denied.");
                    }

                    accountId = snapshot.AccountId;
                    workspaceId = workspaceRequest.WorkspaceId;
                    _tenant.SetWorkspace(accountId.Value, workspaceId.Value, actorId);
                    break;
                }
            case ApplicationScopeKind.Account:
                {
                    if (_credential.Kind == CredentialKind.ApiToken)
                    {
                        throw new ForbiddenException("API tokens are restricted to their bound workspace.");
                    }

                    var actorId = RequireUser(userId, "Account-scoped request requires authenticated user.");
                    if (!accountId.HasValue)
                    {
                        throw new AccountSelectionRequiredException(
                            $"{typeof(TRequest).Name} is account-scoped but no AccountId is selected.");
                    }

                    _tenant.SetAccount(accountId.Value, actorId);
                    break;
                }
            case ApplicationScopeKind.Resource:
                {
                    var actorId = RequireUser(userId, "Resource-scoped request requires authenticated user.");
                    resource = ((IResourceScopedRequest)request).Resource;
                    using var locatorStage = PipelineActivitySource.Instance.StartActivity("resource_locator.query");
                    var location = await _resourceLocator.LocateAsync(resource, actorId, cancellationToken);
                    if (location is null)
                    {
                        throw new AppNotFoundException(resource.Kind.ToString(), resource.ResourceId);
                    }

                    accountId = location.AccountId;
                    workspaceId = location.WorkspaceId;
                    _tenant.SetWorkspace(location.AccountId, location.WorkspaceId, actorId);
                    break;
                }
            default:
                if (_credential.Kind == CredentialKind.ApiToken
                    && descriptor.Principal == ApplicationPrincipalKind.Authenticated)
                {
                    throw new ForbiddenException("API tokens are restricted to their bound workspace.");
                }

                break;
        }

        _executionContext.SetSnapshot(new ExecutionContextSnapshot(
            userId,
            accountId,
            workspaceId,
            resource,
            descriptor.Principal,
            descriptor.Scope,
            _executionContext.CorrelationId.ToString("D")));

        return await next();
    }

    private static Guid RequireUser(Guid? userId, string message) =>
        userId is { } value && value != Guid.Empty
            ? value
            : throw new UnauthorizedException(message);

    private void EnforceApiTokenWorkspaceBinding(Guid workspaceId)
    {
        if (_credential.Kind != CredentialKind.ApiToken)
        {
            return;
        }

        if (_credential.BoundWorkspaceId != workspaceId || !_credential.BoundAccountId.HasValue)
        {
            throw new ForbiddenException("API tokens are restricted to their bound workspace.");
        }
    }
}
