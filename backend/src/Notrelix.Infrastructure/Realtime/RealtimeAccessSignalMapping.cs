using System.Text.Json;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>Maps an access-signal <see cref="RealtimeResourceChangedV1"/> to the notification the gateway receives.</summary>
internal static class RealtimeAccessSignalMapping
{
    public static bool IsAccessSignal(RealtimeResourceChangedV1 change) =>
        string.Equals(change.TopicNamespace, RealtimeAccessSignal.TopicNamespace, StringComparison.Ordinal);

    public static RealtimeNotification ToNotification(RealtimeResourceChangedV1 change, Guid workspaceId) => new(
        EventId: change.EventId.ToString("N"),
        EventType: "access.changed",
        ChangeKind: change.ChangeKind,
        StreamKey: change.StreamKey,
        StreamVersion: Math.Max(1, change.StreamVersion),
        ResourceKind: change.ResourceKind,
        ResourceId: change.ResourceId,
        WorkspaceId: workspaceId,
        CorrelationId: change.CorrelationId.ToString("N"),
        OccurredAt: change.OccurredAt,
        ResourceVersion: 0,
        SchemaVersion: 1,
        PayloadContract: change.PayloadContract,
        SubjectUserId: SubjectOf(change.Payload));

    private static Guid? SubjectOf(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty("userId", out var user)
        && user.ValueKind == JsonValueKind.String
        && Guid.TryParse(user.GetString(), out var id)
            ? id
            : null;
}
