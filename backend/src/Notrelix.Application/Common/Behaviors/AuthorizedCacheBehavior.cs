namespace Notrelix.Application.Common.Behaviors;

public class AuthorizedCacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly IRedisCacheService _cache;
    private readonly CacheKeyFactory _keyFactory;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<AuthorizedCacheBehavior<TRequest, TResponse>> _logger;

    public AuthorizedCacheBehavior(
        IRedisCacheService cache,
        CacheKeyFactory keyFactory,
        ICurrentTenantContext tenantContext,
        ILogger<AuthorizedCacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _keyFactory = keyFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedCacheableRequest cacheable)
            return await next();

        var requestName = typeof(TRequest).FullName!;
        var requestHash = _keyFactory.BuildHash(cacheable.CacheIdentity);

        string cacheKey = cacheable.CacheScope switch
        {
            AuthorizedCacheScope.Account => _keyFactory.Account(
                _tenantContext.RequireAccountId(), requestName, requestHash),

            AuthorizedCacheScope.Workspace => _keyFactory.Workspace(
                _tenantContext.RequireAccountId(), _tenantContext.RequireWorkspaceId(), requestName, requestHash),

            AuthorizedCacheScope.User => _keyFactory.User(
                _tenantContext.RequireAccountId(), _tenantContext.RequireWorkspaceId(),
                _tenantContext.RequireUserId(), requestName, requestHash),

            AuthorizedCacheScope.Permissioned => _keyFactory.Permissioned(
                _tenantContext.RequireAccountId(), _tenantContext.RequireWorkspaceId(),
                _tenantContext.RequireUserId(), "default", requestName, requestHash),

            _ => throw new SecurityMisconfigurationException(
                $"Unknown AuthorizedCacheScope '{cacheable.CacheScope}' on {requestName}.")
        };

        var ttl = cacheable.CacheTtl ?? DefaultTtl;

        var cached = await _cache.GetAsync<TResponse>(cacheKey);
        if (cached is not null)
        {
            _logger.LogTrace(
                "Authorized cache HIT for {CacheKey} ({RequestType})",
                cacheKey, typeof(TRequest).Name);
            return cached;
        }

        _logger.LogTrace(
            "Authorized cache MISS for {CacheKey} ({RequestType})",
            cacheKey, typeof(TRequest).Name);

        var response = await next();

        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ttl);
        }

        return response;
    }
}
