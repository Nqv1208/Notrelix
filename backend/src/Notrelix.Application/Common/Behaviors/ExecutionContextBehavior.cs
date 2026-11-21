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
        using var stage = PipelineActivitySource.Instance.StartActivity("execution_context.resolve");

        var descriptor = _descriptors.GetRequired(typeof(TRequest));
        var userId = _executionContext.UserId ?? _tenant.UserId;
        Guid? accountId = _tenant.AccountId;
        Guid? workspaceId = null;
        ResourceRef? resource = null;
        ResourceLocation? location = null;

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

                    // Not an active member (never joined, suspended, removed): the workspace is as missing as a
                    // made-up id, with the identical NotFound, so its existence does not leak. A member who lacks
                    // the action is refused later with Forbidden: they already know the workspace exists.
                    if (!snapshot.CanAccess)
                    {
                        throw new AppNotFoundException(nameof(Workspace), workspaceRequest.WorkspaceId);
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
                    location = await _resourceLocator.LocateAsync(resource, actorId, cancellationToken);
                    if (location is null)
                    {
                        throw new AppNotFoundException(resource.Kind.ToString(), resource.ResourceId);
                    }

                    // Cross-account resources are hidden, not denied: existence
                    // must not leak through a Forbidden-vs-NotFound distinction
                    // (M7 freeze). The locator resolves by stable identity with
                    // filters bypassed, so the account binding is enforced here
                    // before any tenant context is adopted.
                    if (accountId.HasValue && location.AccountId != accountId.Value)
                    {
                        throw new AppNotFoundException(resource.Kind.ToString(), resource.ResourceId);
                    }

                    // An API token is bound to one workspace: a resource anywhere else, even one the issuing
                    // user may open, is as missing as a made-up id.
                    if (_credential.Kind == CredentialKind.ApiToken
                        && (location.WorkspaceId != _credential.BoundWorkspaceId
                            || location.AccountId != _credential.BoundAccountId))
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

        EnforceApiTokenScope(descriptor, resource, location);

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

    /// <summary>
    /// The one place a token's scopes are checked. A scope only narrows access: it runs after the binding checks
    /// above (so another workspace still answers as before) and before any handler, and the permission checks of
    /// the issuer still follow it. A request no scope covers (descriptor scope null) is refused for every token. A
    /// comment, attachment or reaction takes the scope of the kind of resource it names (board item or page); a kind
    /// no scope maps is refused. The kind is the request's own value, so this reads nothing. A comment or attachment id
    /// takes the scope of the host kind the resource locator returned for it (same read as the NotFound above).
    /// </summary>
    private void EnforceApiTokenScope(RequestDescriptor descriptor, ResourceRef? resource, ResourceLocation? location)
    {
        // Only the tenant-scoped requests are scoped: every other authenticated request is refused to a token
        // above, and an anonymous one (a public form, a sign-in) never needed the credential.
        if (_credential.Kind != CredentialKind.ApiToken
            || descriptor.Scope is not (ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource))
        {
            return;
        }

        var required = descriptor.ApiTokenScopeFollowsLocatedHost
            ? RequiredScopeForLocatedHost(descriptor, resource, location)
            : descriptor.ApiTokenScopeFollowsResource
                ? resource is { } host
                    ? ApiTokenScopeMap.RequiredScopeForResource(host.Kind, descriptor.Kind, descriptor.DataAccess)
                    : null
                : descriptor.ApiTokenScope;

        if (required is null
            || _credential.ApiTokenScopes?.Contains(required) != true)
        {
            throw new ForbiddenException(
                "This API token does not have the scope this request needs.",
                AccessDenialCodes.ApiTokenScopeMissing);
        }
    }

    /// <summary>
    /// The scope of a request keyed by a comment or attachment id: from the host kind the locator already returned.
    /// A host of another kind than the one the request accepts is as missing as a made-up id (the same NotFound).
    /// </summary>
    private static string? RequiredScopeForLocatedHost(RequestDescriptor descriptor, ResourceRef? resource, ResourceLocation? location)
    {
        if (location?.HostKind is not { } hostKind || resource is null)
        {
            return null;
        }

        var accepted = ApiTokenScopeMap.LocatedHostRequests.GetValueOrDefault(descriptor.RequestType.FullName ?? string.Empty);
        if (accepted is not null && hostKind.Value != accepted)
        {
            throw new AppNotFoundException(resource.Kind.ToString(), resource.ResourceId);
        }

        return ApiTokenScopeMap.RequiredScopeForResource(hostKind, descriptor.Kind, descriptor.DataAccess);
    }

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
