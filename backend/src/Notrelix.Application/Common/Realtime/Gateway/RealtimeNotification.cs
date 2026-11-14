using System.Text.Json;

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
/// <param name="SubjectUserId">An access signal only: the person whose access changed, or null for everyone it concerns.</param>
/// <param name="Payload">
/// A compact semantic payload, carried only for the topics that opt in (Chat: channel messages and the per-user
/// unread stream). Null for every other topic, so their frames stay notify-only.
/// </param>
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
    string PayloadContract,
    Guid? SubjectUserId = null,
    JsonElement? Payload = null);

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
public sealed record RealtimeSubscriptionGrant(Guid AccountId, Guid WorkspaceId)
{
    /// <summary>
    /// The grant of a per-user stream: it spans the person's workspaces, so it is bound to none. The gateway checks
    /// such a subscription against the person named in each notification, not against a workspace.
    /// </summary>
    public static readonly RealtimeSubscriptionGrant Unbound = new(Guid.Empty, Guid.Empty);
}

/// <summary>
/// Source of access-change signals for one workspace. An access signal is not delivered to clients: it
/// makes every gateway connection of the affected person re-authorize its subscriptions at once, so a
/// removed member or a lowered role is cut within about a second instead of at the next periodic pass.
/// Delivered on every gateway instance (in process in Development, through Redis otherwise).
/// </summary>
public interface IRealtimeAccessSignalSource
{
    Task<IAsyncDisposable> SubscribeAccessAsync(
        Guid workspaceId,
        Func<RealtimeNotification, ValueTask> onSignal,
        CancellationToken cancellationToken);
}

/// <summary>The shape of an access signal inside <see cref="RealtimeResourceChangedV1"/>.</summary>
public static class RealtimeAccessSignal
{
    public const string TopicNamespace = "access";
    public const string ChangeKind = "AccessChanged";
    public const string PayloadContract = "access.changed.v1";
    /// <summary>The signal concerns one Board (resource id = the Board), or the whole workspace.</summary>
    public const string BoardResource = "Board";
    public const string WorkspaceResource = "Workspace";
    /// <summary>The signal concerns one Chat channel (resource id = the space id).</summary>
    public const string ChannelResource = "Channel";
}
