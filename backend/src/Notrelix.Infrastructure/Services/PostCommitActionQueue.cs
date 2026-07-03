using Notrelix.Application.Common.Context;

namespace Notrelix.Infrastructure.Services;

public sealed class PostCommitActionQueue : IPostCommitActionQueue
{
    private readonly List<CacheInvalidationAction> _cacheInvalidations = new();
    private readonly List<RealtimeAction> _realtimeActions = new();

    public IReadOnlyList<CacheInvalidationAction> CacheInvalidations => _cacheInvalidations;
    public IReadOnlyList<RealtimeAction> RealtimeActions => _realtimeActions;

    public void EnqueueCacheInvalidation(CacheInvalidationAction action)
    {
        _cacheInvalidations.Add(action);
    }

    public void EnqueueRealtime(RealtimeAction action)
    {
        _realtimeActions.Add(action);
    }

    public void Clear()
    {
        _cacheInvalidations.Clear();
        _realtimeActions.Clear();
    }
}
