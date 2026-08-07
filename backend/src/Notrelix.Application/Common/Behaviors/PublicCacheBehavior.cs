using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public class PublicCacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly IRedisCacheService _cache;
    private readonly CacheKeyFactory _keyFactory;
    private readonly ILogger<PublicCacheBehavior<TRequest, TResponse>> _logger;

    public PublicCacheBehavior(
        IRedisCacheService cache,
        CacheKeyFactory keyFactory,
        ILogger<PublicCacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _keyFactory = keyFactory;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IPublicCacheableQuery<TResponse> cacheable)
            return await next();

        var profile = RequestExecutionClassifier.Classify(request);
        if (profile.IsTenantScoped)
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} cannot use public cache for tenant-scoped data. " +
                "Use AuthorizedCacheBehavior for private/tenant-scoped cache instead.");
        }

        var requestName = typeof(TRequest).FullName!;
        var requestHash = _keyFactory.BuildHash(cacheable.CacheIdentity);
        var cacheKey = _keyFactory.Public(requestName, requestHash);
        var ttl = cacheable.Ttl ?? DefaultTtl;

        try
        {
            var cached = await _cache.GetAsync<TResponse>(cacheKey);
            if (cached is not null)
            {
                _logger.LogTrace("Cache HIT for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);
                return cached;
            }

            _logger.LogTrace("Cache MISS for {CacheKey} ({RequestType})", cacheKey, typeof(TRequest).Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Public cache unavailable for {RequestType} — bypassing cache", typeof(TRequest).Name);
        }

        var response = await next();

        if (response is not null)
        {
            try
            {
                await _cache.SetAsync(cacheKey, response, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Public cache write unavailable for {RequestType} — response served without caching", typeof(TRequest).Name);
            }
        }

        return response;
    }
}
