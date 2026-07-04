namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Authorized cache behavior. Runs inside DB/RLS scope, AFTER authorization.
/// For IAuthorizedCacheableRequest: cache-first for private/user-scoped data.
/// Unlike CacheBehavior (public), this runs after auth so cached data is user-specific.
/// </summary>
public class AuthorizedCacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<AuthorizedCacheBehavior<TRequest, TResponse>> _logger;

    public AuthorizedCacheBehavior(
        IRedisCacheService cache,
        ILogger<AuthorizedCacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedCacheableRequest cacheable)
            return await next();

        var cacheKey = cacheable.AuthorizedCacheKey;
        var ttl = cacheable.AuthorizedCacheTtl;

        // Try cache first (after authorization has already passed)
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

        // Store in cache
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ttl);
        }

        return response;
    }
}
