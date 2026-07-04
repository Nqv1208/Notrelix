namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Public/shared cache behavior. Runs BEFORE DB/RLS scope (outer zone).
/// For ICacheableQuery requests: check Redis cache first, store result on cache miss.
/// This is for PUBLIC data only. Private/user-scoped cache uses AuthorizedCacheBehavior.
/// </summary>
public class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<CacheBehavior<TRequest, TResponse>> _logger;

    public CacheBehavior(
        IRedisCacheService cache,
        ILogger<CacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery<TResponse> cacheable)
            return await next();

        var cacheKey = cacheable.CacheKey;
        var ttl = cacheable.Ttl;

        // Try cache first
        var cached = await _cache.GetAsync<TResponse>(cacheKey);
        if (cached is not null)
        {
            _logger.LogTrace("Cache HIT for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);
            return cached;
        }

        _logger.LogTrace("Cache MISS for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);

        var response = await next();

        // Store in cache
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ttl);
        }

        return response;
    }
}
