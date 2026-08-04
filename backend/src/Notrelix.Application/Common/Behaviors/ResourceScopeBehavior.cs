using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Application.Common.Behaviors;

public class ResourceScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantContext _tenant;
    private readonly IResourceScopeResolver _resolver;
    private readonly ILogger<ResourceScopeBehavior<TRequest, TResponse>> _logger;

    public ResourceScopeBehavior(
        ICurrentTenantContext tenant,
        IResourceScopeResolver resolver,
        ILogger<ResourceScopeBehavior<TRequest, TResponse>> logger)
    {
        _tenant = tenant;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IResourceScopedRequest resourceRequest)
            return await next();

        var actorUserId = _tenant.UserId
            ?? throw new UnauthorizedException("Resource-scoped request requires authenticated user.");
        var snapshot = await _resolver.ResolveAsync(resourceRequest.Resource, actorUserId, ct);

        if (snapshot is null)
        {
            _logger.LogWarning(
                "Resource not found: Kind={ResourceKind} Id={ResourceId} RequestType={RequestType}",
                resourceRequest.Resource.Kind,
                resourceRequest.Resource.ResourceId,
                typeof(TRequest).Name);

            throw new AppNotFoundException(resourceRequest.Resource.Kind.ToString(), resourceRequest.Resource.ResourceId);
        }

        _logger.LogTrace(
            "Resolved resource scope: AccountId={AccountId} WorkspaceId={WorkspaceId} ResourceKind={ResourceKind} ResourceId={ResourceId}",
            snapshot.AccountId,
            snapshot.WorkspaceId,
            snapshot.ResourceKind,
            snapshot.ResourceId);

        _tenant.SetWorkspace(snapshot.AccountId, snapshot.WorkspaceId, actorUserId);

        return await next();
    }
}
