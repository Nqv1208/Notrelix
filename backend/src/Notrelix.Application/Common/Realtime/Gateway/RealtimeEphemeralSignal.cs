namespace Notrelix.Application.Common.Realtime.Gateway;

/// <summary>The kinds of ephemeral signal the gateway fans out between connections. None is ever stored.</summary>
public static class RealtimeEphemeralKinds
{
    /// <summary>A person is typing in a space.</summary>
    public const string Typing = "typing";

    /// <summary>A person's presence state changed or was refreshed.</summary>
    public const string Presence = "presence";

    /// <summary>A newly subscribed connection asks the others of the space to announce their presence once.</summary>
    public const string PresenceRequest = "presence-request";
}

/// <summary>The presence states a client can report.</summary>
public static class RealtimePresenceStates
{
    public const string Online = "online";
    public const string Idle = "idle";
    public const string Offline = "offline";

    public static bool IsValid(string? state) => state is Online or Idle or Offline;
}

/// <summary>
/// An ephemeral gateway signal (typing, presence). It is delivered to the subscribers of one scope and is never
/// persisted, replayed or used for authorization. <paramref name="To"/> narrows a presence answer to the person who asked.
/// </summary>
public sealed record RealtimeEphemeralSignal(
    string Kind,
    Guid WorkspaceId,
    Guid UserId,
    string? State = null,
    Guid? To = null);

/// <summary>
/// Fan-out of ephemeral signals between the gateway connections of a scope, across instances. Publishing reaches the
/// handlers on every instance (in process in Development, through Redis otherwise); nothing is buffered, so a
/// signal published while nobody listens is gone.
/// </summary>
public interface IRealtimeEphemeralBus
{
    Task PublishAsync(RealtimeScope scope, RealtimeEphemeralSignal signal, CancellationToken cancellationToken);

    Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeEphemeralSignal, ValueTask> onSignal,
        CancellationToken cancellationToken);
}
