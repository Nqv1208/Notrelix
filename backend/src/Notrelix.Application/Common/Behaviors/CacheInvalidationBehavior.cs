using MediatR;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;

namespace Notrelix.Application.Common.Behaviors;

public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(IRedisCacheService cacheService, ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IInvalidateCacheRequest cacheInvalidationRequest)
        {
            var response = await next();

            var keys = cacheInvalidationRequest.GetInvalidationKeys();
            foreach (var key in keys)
            {
                _logger.LogTrace("Invalidating cache key pattern: {Pattern}", key.Pattern);
                await _cacheService.RemoveAsync(key.Pattern);
            }

            return response;
        }

        return await next();
    }
}
