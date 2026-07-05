namespace Notrelix.Application.Common.Behaviors;

public class ResourceScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantContext _tenant;
    private readonly ITenantBootstrapStore _bootstrapStore;
    private readonly ILogger<ResourceScopeBehavior<TRequest, TResponse>> _logger;

    public ResourceScopeBehavior(
        ICurrentTenantContext tenant,
        ITenantBootstrapStore bootstrapStore,
        ILogger<ResourceScopeBehavior<TRequest, TResponse>> logger)
    {
        _tenant = tenant;
        _bootstrapStore = bootstrapStore;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IResourceScopedRequest resourceRequest)
            return await next();

        var snapshot = await _bootstrapStore.ResolveResourceContextAsync(
            resourceRequest.ResourceId, resourceRequest.ResourceType, ct);

        if (snapshot is null)
        {
            _logger.LogWarning(
                "Resource not found: Type={ResourceType} Id={ResourceId} RequestType={RequestType}",
                resourceRequest.ResourceType,
                resourceRequest.ResourceId,
                typeof(TRequest).Name);

            throw new NotFoundException(resourceRequest.ResourceType, resourceRequest.ResourceId);
        }

        var userId = _tenant.UserId;
        _logger.LogTrace(
            "Resolved resource scope: AccountId={AccountId} WorkspaceId={WorkspaceId} ResourceType={ResourceType} ResourceId={ResourceId}",
            snapshot.AccountId,
            snapshot.WorkspaceId,
            resourceRequest.ResourceType,
            resourceRequest.ResourceId);

        _tenant.SetWorkspace(snapshot.AccountId, snapshot.WorkspaceId, userId);

        return await next();
    }
}
