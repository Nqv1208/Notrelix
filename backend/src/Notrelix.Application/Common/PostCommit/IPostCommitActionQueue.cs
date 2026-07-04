namespace Notrelix.Application.Common.PostCommit;

public interface IPostCommitActionQueue
{
    void BeginScope();
    void EnqueueCacheInvalidation(CacheInvalidationAction action);
    void EnqueueRealtime(RealtimeAction action);

    IReadOnlyList<CacheInvalidationAction> CacheInvalidations { get; }
    IReadOnlyList<RealtimeAction> RealtimeActions { get; }

    Task FlushAsync(CancellationToken ct);
    void Clear();
    void EndScope();
}

public sealed record CacheInvalidationAction(
    string Key,
    Guid? AccountId,
    Guid? WorkspaceId);

public sealed record RealtimeAction(
    RealtimeTopic Topic,
    object Payload,
    Guid? AccountId,
    Guid? WorkspaceId,
    IReadOnlyCollection<Guid> UserIds);
