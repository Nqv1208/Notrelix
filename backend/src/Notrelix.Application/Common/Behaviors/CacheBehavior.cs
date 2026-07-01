namespace Notrelix.Application.Common.Behaviors;

public class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<CacheBehavior<TRequest, TResponse>> _logger;

    public CacheBehavior(IRedisCacheService cacheService, ILogger<CacheBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ICacheableQuery<TResponse> cacheableQuery)
        {
            var cached = await _cacheService.GetAsync<TResponse>(cacheableQuery.CacheKey);
            if (cached is not null)
            {
                _logger.LogTrace("Cache hit for {CacheKey}", cacheableQuery.CacheKey);
                return cached;
            }

            _logger.LogTrace("Cache miss for {CacheKey}", cacheableQuery.CacheKey);

            var response = await next();

            await _cacheService.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.Ttl);

            return response;
        }

        return await next();
    }
}
