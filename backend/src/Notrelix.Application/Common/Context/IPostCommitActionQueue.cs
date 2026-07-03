namespace Notrelix.Application.Common.Context;

public interface IPostCommitActionQueue
{
    void EnqueueCacheInvalidation(CacheInvalidationAction action);
    void EnqueueRealtime(RealtimeAction action);

    IReadOnlyList<CacheInvalidationAction> CacheInvalidations { get; }
    IReadOnlyList<RealtimeAction> RealtimeActions { get; }

    void Clear();
}

public sealed record CacheInvalidationAction(
    string Key,
    Guid? AccountId,
    Guid? WorkspaceId);

public sealed record RealtimeAction(
    string Topic,
    object Payload,
    Guid? AccountId,
    Guid? WorkspaceId,
    IReadOnlyCollection<Guid> UserIds);
