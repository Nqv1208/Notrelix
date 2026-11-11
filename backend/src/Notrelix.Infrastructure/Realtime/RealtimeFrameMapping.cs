using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// How a durable realtime fact becomes a gateway notification. Shared by every publisher so the Redis and the
/// in-process transports agree on the scope, the event type and the payload of a frame.
/// </summary>
internal static class RealtimeFrameMapping
{
    /// <summary>Topics whose frames carry a compact payload. Every other topic stays notify-only.</summary>
    private static bool CarriesPayload(string topicNamespace) =>
        topicNamespace is "chat" or "user";

    /// <summary>The scope a fact is delivered to, or null when no gateway scope serves its topic.</summary>
    public static RealtimeScope? ScopeOf(RealtimeResourceChangedV1 change) => change.TopicNamespace switch
    {
        "workspace" => new RealtimeScope(RealtimeScopeKind.Workspace, change.ResourceId),
        "board" => new RealtimeScope(RealtimeScopeKind.Board, change.ResourceId),
        "chat" => new RealtimeScope(RealtimeScopeKind.Channel, change.ResourceId),
        "user" => new RealtimeScope(RealtimeScopeKind.User, change.ResourceId),
        _ => null,
    };

    /// <summary>The event type a client switches on. Chat frames name themselves (chat.message.created, ...).</summary>
    public static string EventTypeOf(RealtimeResourceChangedV1 change) =>
        CarriesPayload(change.TopicNamespace)
            ? change.ChangeKind
            : $"{change.TopicNamespace}.{change.ResourceKind}".ToLowerInvariant();

    /// <summary>The payload a frame carries; null for notify-only topics.</summary>
    public static JsonElement? PayloadOf(RealtimeResourceChangedV1 change) =>
        CarriesPayload(change.TopicNamespace) ? change.Payload : null;
}
