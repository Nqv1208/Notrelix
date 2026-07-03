using Notrelix.Application.Common.Context;

namespace Notrelix.Application.Common.Behaviors;

public class PostCommitActionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPostCommitActionQueue _queue;
    private readonly ILogger<PostCommitActionBehavior<TRequest, TResponse>> _logger;

    public PostCommitActionBehavior(
        IPostCommitActionQueue queue,
        ILogger<PostCommitActionBehavior<TRequest, TResponse>> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        var invalidationCount = _queue.CacheInvalidations.Count;
        var realtimeCount = _queue.RealtimeActions.Count;

        if (invalidationCount > 0)
        {
            _logger.LogTrace("Processing {Count} cache invalidations for {RequestType}",
                invalidationCount, typeof(TRequest).Name);

            foreach (var inv in _queue.CacheInvalidations)
                _logger.LogDebug("Cache invalidation: {Key}", inv.Key);
        }

        if (realtimeCount > 0)
        {
            _logger.LogTrace("Processing {Count} realtime actions for {RequestType}",
                realtimeCount, typeof(TRequest).Name);

            foreach (var action in _queue.RealtimeActions)
                _logger.LogDebug("Realtime: {Topic}", action.Topic);
        }

        if (invalidationCount > 0 || realtimeCount > 0)
            _queue.Clear();

        return response;
    }
}
