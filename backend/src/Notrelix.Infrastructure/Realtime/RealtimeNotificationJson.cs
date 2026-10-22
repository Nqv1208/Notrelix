using System.Text.Json;

using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Internal Redis wire format of <see cref="RealtimeNotification"/>. The stream
/// version is the last property so the publisher can splice the atomically
/// allocated counter into the message inside the Redis script.
/// </summary>
public static class RealtimeNotificationJson
{
    private const string VersionProperty = ",\"streamVersion\":";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Returns the message split around the stream version placeholder.</summary>
    public static (string Prefix, string Suffix) SerializeForVersionSplice(RealtimeNotification draft)
    {
        var json = JsonSerializer.Serialize(Wire.From(draft), Options);
        // Wire ends with "...,"streamVersion":0}" — cut before the placeholder value.
        var cut = json.LastIndexOf(VersionProperty, StringComparison.Ordinal);
        return (json[..(cut + VersionProperty.Length)], "}");
    }

    public static string Serialize(RealtimeNotification notification)
    {
        var (prefix, suffix) = SerializeForVersionSplice(notification);
        return prefix + notification.StreamVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix;
    }

    public static bool TryDeserialize(string json, out RealtimeNotification notification)
    {
        notification = null!;
        try
        {
            var wire = JsonSerializer.Deserialize<Wire>(json, Options);
            if (wire is null
                || string.IsNullOrEmpty(wire.EventId)
                || string.IsNullOrEmpty(wire.StreamKey)
                || wire.StreamVersion < 1
                || wire.WorkspaceId == Guid.Empty
                || wire.SchemaVersion != 1)
            {
                return false;
            }

            notification = wire.ToNotification();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record Wire(
        string EventId,
        string EventType,
        string ChangeKind,
        string StreamKey,
        string ResourceKind,
        Guid ResourceId,
        Guid WorkspaceId,
        string CorrelationId,
        DateTimeOffset OccurredAt,
        long ResourceVersion,
        int SchemaVersion,
        string PayloadContract,
        long StreamVersion)
    {
        public static Wire From(RealtimeNotification n) => new(
            n.EventId, n.EventType, n.ChangeKind, n.StreamKey, n.ResourceKind, n.ResourceId, n.WorkspaceId,
            n.CorrelationId, n.OccurredAt, n.ResourceVersion, n.SchemaVersion, n.PayloadContract, 0);

        public RealtimeNotification ToNotification() => new(
            EventId, EventType, ChangeKind, StreamKey, StreamVersion, ResourceKind, ResourceId, WorkspaceId,
            CorrelationId, OccurredAt, ResourceVersion, SchemaVersion, PayloadContract);
    }
}
