namespace Notrelix.Infrastructure.Services;

public sealed class PostCommitActionQueue : IPostCommitActionQueue
{
    private readonly IRedisCacheService _cache;
    private readonly IRealtimePublisher _publisher;
    private readonly ILogger<PostCommitActionQueue> _logger;
    private readonly List<CacheInvalidationAction> _cacheInvalidations = new();
    private readonly List<RealtimeAction> _realtimeActions = new();
    private bool _isInScope;

    public PostCommitActionQueue(
        IRedisCacheService cache,
        IRealtimePublisher publisher,
        ILogger<PostCommitActionQueue> logger)
    {
        _cache = cache;
        _publisher = publisher;
        _logger = logger;
    }

    public IReadOnlyList<CacheInvalidationAction> CacheInvalidations => _cacheInvalidations;
    public IReadOnlyList<RealtimeAction> RealtimeActions => _realtimeActions;

    public void BeginScope() => _isInScope = true;

    public void EnqueueCacheInvalidation(CacheInvalidationAction action)
    {
        _cacheInvalidations.Add(action);
    }

    public void EnqueueRealtime(RealtimeAction action)
    {
        _realtimeActions.Add(action);
    }

    public async Task FlushAsync(CancellationToken ct)
    {
        if (!_isInScope) return;

        foreach (var inv in _cacheInvalidations)
        {
            _logger.LogTrace("Flushing cache invalidation: {Key}", inv.Key);
            await _cache.RemoveAsync(inv.Key);
        }

        foreach (var action in _realtimeActions)
        {
            _logger.LogTrace("Flushing realtime: {Namespace}/{ResourceType}/{ResourceId}",
                action.Topic.Namespace, action.Topic.ResourceType, action.Topic.ResourceId);
            await _publisher.PublishAsync(action.Topic, action.Payload, ct);
        }

        Clear();
    }

    public void Clear()
    {
        _cacheInvalidations.Clear();
        _realtimeActions.Clear();
    }

    public void EndScope() => _isInScope = false;
}
