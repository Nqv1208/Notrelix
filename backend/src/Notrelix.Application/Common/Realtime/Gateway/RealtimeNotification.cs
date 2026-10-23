namespace Notrelix.Application.Common.Realtime.Gateway;

/// <summary>
/// A notify-only realtime fact delivered to subscribers. It carries identity and
/// ordering metadata, never resource data: clients refetch authoritative state
/// over REST, where resource authorization is re-evaluated.
/// </summary>
/// <param name="StreamKey">Stable stream identity (see <see cref="RealtimeScope.StreamKey"/>).</param>
/// <param name="StreamVersion">
/// Per-stream delivery counter allocated atomically with publication; consecutive
/// per stream. Any value other than previous + 1 means the client must refetch.
/// </param>
/// <param name="ResourceVersion">Version of the changed resource at commit; informational only.</param>
public sealed record RealtimeNotification(
    string EventId,
    string EventType,
    string ChangeKind,
    string StreamKey,
    long StreamVersion,
    string ResourceKind,
    Guid ResourceId,
    Guid WorkspaceId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    long ResourceVersion,
    int SchemaVersion,
    string PayloadContract);

/// <summary>
/// Source of realtime notifications for one scope. Implementations share the
/// underlying transport subscription between handlers; handlers must not block.
/// </summary>
public interface IRealtimeNotificationSource
{
    Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeNotification, ValueTask> onNotification,
        CancellationToken cancellationToken);
}

/// <summary>Resolved tenant binding of an authorized subscription.</summary>
public sealed record RealtimeSubscriptionGrant(Guid AccountId, Guid WorkspaceId);
