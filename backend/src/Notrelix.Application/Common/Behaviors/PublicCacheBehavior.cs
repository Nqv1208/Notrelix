using Notrelix.Application.Common.CQRS.Execution;

namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Public/shared cache behavior. Runs BEFORE DB/RLS scope (outer zone).
/// For IPublicCacheableQuery requests: check Redis cache first, store result on cache miss.
/// This is for PUBLIC data only. Private/user-scoped cache uses AuthorizedCacheBehavior.
/// </summary>
public class PublicCacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<PublicCacheBehavior<TRequest, TResponse>> _logger;

    public PublicCacheBehavior(
        IRedisCacheService cache,
        ILogger<PublicCacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IPublicCacheableQuery<TResponse> cacheable)
            return await next();

        // Defense-in-depth: ensure public cache is never used for tenant-scoped data.
        // RequestContractGuardBehavior (runs earlier) also guards this, but this check
        // protects against pipeline order changes or new behaviors added before the guard.
        var profile = RequestExecutionClassifier.Classify(request);
        if (profile.IsTenantScoped)
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} cannot use public cache for tenant-scoped data. " +
                "Use AuthorizedCacheBehavior for private/tenant-scoped cache instead.");
        }

        var cacheKey = cacheable.CacheKey;
        var ttl = cacheable.Ttl;

        var cached = await _cache.GetAsync<TResponse>(cacheKey);
        if (cached is not null)
        {
            _logger.LogTrace("Cache HIT for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);
            return cached;
        }

        _logger.LogTrace("Cache MISS for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);

        var response = await next();

        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ttl);
        }

        return response;
    }
}
