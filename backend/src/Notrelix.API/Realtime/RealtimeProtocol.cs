using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Realtime;

/// <summary>
/// Wire protocol of the realtime gateway (schema version 1). All frames are UTF-8
/// JSON text. Realtime only notifies: <c>event</c> frames carry stream identity and
/// ordering metadata and never resource data. A client keeps the last
/// <c>streamVersion</c> per <c>streamKey</c>; any version other than previous + 1
/// (gap, duplicate, reset) means it must refetch authoritative state over REST.
/// </summary>
public static class RealtimeProtocol
{
    public const int SchemaVersion = 1;

    public const int MaxSubscriptionIdLength = 64;

    public static class ClientTypes
    {
        public const string Subscribe = "subscribe";
        public const string Unsubscribe = "unsubscribe";
        public const string Ping = "ping";
        public const string Pong = "pong";
    }

    public static class ServerTypes
    {
        public const string Ready = "ready";
        public const string Subscribed = "subscribed";
        public const string Unsubscribed = "unsubscribed";
        public const string SubscriptionError = "subscription-error";
        public const string Event = "event";
        public const string Ping = "ping";
        public const string Pong = "pong";
    }

    /// <summary>Stable error codes carried by <c>subscription-error</c> frames.</summary>
    public static class ErrorCodes
    {
        public const string InvalidMessage = "invalid_message";
        public const string UnsupportedScope = "unsupported_scope";
        public const string SubscriptionDenied = "subscription_denied";
        public const string SubscriptionLimit = "subscription_limit";
        public const string SubscriptionConflict = "subscription_conflict";
        public const string SubscriptionRevoked = "subscription_revoked";
        public const string AuthorizationUnavailable = "authorization_unavailable";
        public const string UnknownSubscription = "unknown_subscription";
    }

    /// <summary>WebSocket close codes the gateway uses (4xxx are application-defined).</summary>
    public static class CloseCodes
    {
        public const WebSocketCloseStatus Normal = WebSocketCloseStatus.NormalClosure;
        public const WebSocketCloseStatus GoingAway = WebSocketCloseStatus.EndpointUnavailable;
        public const WebSocketCloseStatus UnsupportedData = WebSocketCloseStatus.InvalidMessageType;
        public const WebSocketCloseStatus PolicyViolation = WebSocketCloseStatus.PolicyViolation;
        public const WebSocketCloseStatus MessageTooBig = WebSocketCloseStatus.MessageTooBig;
        public const WebSocketCloseStatus SlowConsumer = (WebSocketCloseStatus)1013;
        public const WebSocketCloseStatus CredentialsInvalid = (WebSocketCloseStatus)4401;
        public const WebSocketCloseStatus HeartbeatTimeout = (WebSocketCloseStatus)4408;
    }

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Client → server ─────────────────────────────────────

    public abstract record ClientMessage;

    public sealed record SubscribeMessage(string SubscriptionId, RealtimeScope Scope) : ClientMessage;

    public sealed record UnsubscribeMessage(string SubscriptionId) : ClientMessage;

    public sealed record PingMessage(string? SentAt) : ClientMessage;

    public sealed record PongMessage : ClientMessage;

    public sealed record InvalidMessage(string Code, string? SubscriptionId = null) : ClientMessage;

    /// <summary>Parses one client text frame. Never throws; malformed input becomes <see cref="InvalidMessage"/>.</summary>
    public static ClientMessage ParseClientMessage(ReadOnlySpan<byte> utf8)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8.ToArray());
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String)
            {
                return new InvalidMessage(ErrorCodes.InvalidMessage);
            }

            var subscriptionId = ReadString(root, "subscriptionId");
            switch (typeElement.GetString())
            {
                case ClientTypes.Subscribe:
                    if (!IsValidSubscriptionId(subscriptionId))
                    {
                        return new InvalidMessage(ErrorCodes.InvalidMessage);
                    }

                    if (!root.TryGetProperty("scope", out var scopeElement)
                        || scopeElement.ValueKind != JsonValueKind.Object)
                    {
                        return new InvalidMessage(ErrorCodes.InvalidMessage, subscriptionId);
                    }

                    return RealtimeScope.TryParse(ReadString(scopeElement, "kind"), ReadString(scopeElement, "id"), out var scope)
                        ? new SubscribeMessage(subscriptionId!, scope)
                        : new InvalidMessage(ErrorCodes.UnsupportedScope, subscriptionId);
                case ClientTypes.Unsubscribe:
                    return IsValidSubscriptionId(subscriptionId)
                        ? new UnsubscribeMessage(subscriptionId!)
                        : new InvalidMessage(ErrorCodes.InvalidMessage);
                case ClientTypes.Ping:
                    return new PingMessage(ReadString(root, "sentAt"));
                case ClientTypes.Pong:
                    return new PongMessage();
                default:
                    return new InvalidMessage(ErrorCodes.InvalidMessage);
            }
        }
        catch (JsonException)
        {
            return new InvalidMessage(ErrorCodes.InvalidMessage);
        }
    }

    public static bool IsValidSubscriptionId(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxSubscriptionIdLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // ── Server → client ─────────────────────────────────────

    public static byte[] Ready(Guid connectionId, RealtimeGatewayOptions options) => Serialize(new
    {
        type = ServerTypes.Ready,
        schemaVersion = SchemaVersion,
        connectionId = connectionId.ToString("N"),
        pingIntervalMs = (long)options.PingInterval.TotalMilliseconds,
        idleTimeoutMs = (long)options.IdleTimeout.TotalMilliseconds,
        maxSubscriptions = options.MaxSubscriptionsPerConnection,
    });

    public static byte[] Subscribed(string subscriptionId, RealtimeScope scope) => Serialize(new
    {
        type = ServerTypes.Subscribed,
        subscriptionId,
        scope = new { kind = scope.WireKind, id = scope.Id },
        streamKey = scope.StreamKey,
    });

    public static byte[] Unsubscribed(string subscriptionId) => Serialize(new
    {
        type = ServerTypes.Unsubscribed,
        subscriptionId,
    });

    public static byte[] SubscriptionError(string? subscriptionId, string code) => Serialize(new
    {
        type = ServerTypes.SubscriptionError,
        subscriptionId,
        code,
    });

    public static byte[] Ping(DateTimeOffset sentAt) => Serialize(new
    {
        type = ServerTypes.Ping,
        sentAt = sentAt.ToString("O"),
    });

    public static byte[] Pong(string? sentAt) => Serialize(new
    {
        type = ServerTypes.Pong,
        sentAt,
    });

    public static byte[] Event(string subscriptionId, RealtimeNotification notification) => Serialize(new
    {
        type = ServerTypes.Event,
        subscriptionId,
        schemaVersion = notification.SchemaVersion,
        eventId = notification.EventId,
        eventType = notification.EventType,
        changeKind = notification.ChangeKind,
        streamKey = notification.StreamKey,
        streamVersion = notification.StreamVersion,
        resourceKind = notification.ResourceKind,
        resourceId = notification.ResourceId,
        resourceVersion = notification.ResourceVersion,
        workspaceId = notification.WorkspaceId,
        correlationId = notification.CorrelationId,
        occurredAt = notification.OccurredAt.ToString("O"),
        payloadContract = notification.PayloadContract,
    });

    private static byte[] Serialize<T>(T value) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions));
}
