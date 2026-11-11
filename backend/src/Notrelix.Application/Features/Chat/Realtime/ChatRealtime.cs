using System.Text;
using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Realtime;

/// <summary>
/// Chat's realtime facts (SYS-ADR-001, protocol v1.1, UC-CHT-C02, C03, C04, C09). A message fact becomes a
/// <see cref="RealtimeResourceChangedV1"/> on the channel stream of its space, and a counter change becomes one on the
/// per-user stream. Both carry a compact payload; REST stays the source of truth and clients recover by the sequence.
/// </summary>
public static class ChatRealtime
{
    public const string ChannelTopicNamespace = "chat";
    public const string ChannelResourceKind = "Channel";
    public const string UserTopicNamespace = "user";
    public const string UserResourceKind = "notifications";

    public const string MessageCreatedEvent = "chat.message.created";
    public const string MessageUpdatedEvent = "chat.message.updated";
    public const string MessageDeletedEvent = "chat.message.deleted";
    public const string UnreadChangedEvent = "chat.unread.changed";

    public const string MessagePayloadContract = "chat.message.v1";
    public const string UnreadPayloadContract = "chat.unread.v1";

    /// <summary>
    /// The largest message payload in bytes. The frame envelope (ids, stream key, names, timestamps) is under 1 KiB,
    /// so a payload this size keeps the whole frame under the 4096-byte limit. Content that does not fit is left out
    /// (contentOmitted = true) and the client fetches it by sequence.
    /// </summary>
    public const int MaxPayloadBytes = 3000;

    /// <summary>
    /// The most recipients one message's unread fan-out reaches (one realtime publish each). A channel above it is
    /// still delivered on the channel stream; its badges are refreshed by the next read-state call or reconnect.
    /// </summary>
    public const int MaxUnreadRecipients = 2000;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>The message payload of the channel stream (contract <see cref="MessagePayloadContract"/>).</summary>
    public sealed record MessageFrame(
        Guid Id,
        long Sequence,
        Guid AuthorId,
        string Kind,
        string? Content,
        bool ContentOmitted,
        bool Deleted,
        DateTimeOffset? CreatedAt,
        DateTimeOffset? EditedAt,
        Guid? ReplyToId,
        long Version);

    /// <summary>The per-user unread payload (contract <see cref="UnreadPayloadContract"/>).</summary>
    public sealed record UnreadFrame(Guid SpaceId, int Unread, int Mentions);

    public static RealtimeResourceChangedV1 ForCreated(ChatMessageCreatedIntegrationEvent e, DateTimeOffset now)
    {
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, e.Kind, e.Content, ContentOmitted: false, Deleted: false,
            CreatedAt: e.OccurredAt, EditedAt: null, ReplyToId: e.ReplyToId, Version: e.Version);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageCreatedEvent, Fit(frame), now);
    }

    public static RealtimeResourceChangedV1 ForUpdated(ChatMessageUpdatedIntegrationEvent e, DateTimeOffset now)
    {
        // Only a user message is edited (a system message is immutable), so the kind is known.
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, MessageKindWire.UserKind, e.Content, ContentOmitted: false, Deleted: false,
            CreatedAt: null, EditedAt: e.OccurredAt, ReplyToId: null, Version: e.Version);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageUpdatedEvent, Fit(frame), now);
    }

    public static RealtimeResourceChangedV1 ForDeleted(ChatMessageDeletedIntegrationEvent e, DateTimeOffset now)
    {
        // A tombstone keeps its id, sequence, author and version; the content is gone (null).
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, MessageKindWire.UserKind, Content: null, ContentOmitted: false, Deleted: true,
            CreatedAt: null, EditedAt: null, ReplyToId: null, Version: e.Version);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageDeletedEvent, Serialize(frame), now);
    }

    /// <summary>One space's unread counts for one person, for the per-user stream (the badge refresh).</summary>
    public static RealtimeResourceChangedV1 Unread(
        Guid? accountId, Guid workspaceId, Guid userId, Guid spaceId, int unread, int mentions,
        Guid? correlationId, Guid? causationId, DateTimeOffset now)
    {
        var scope = new RealtimeScope(RealtimeScopeKind.User, userId);
        return new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            accountId,
            workspaceId,
            userId,
            correlationId is { } c && c != Guid.Empty ? c : Guid.NewGuid(),
            causationId,
            now,
            UserTopicNamespace,
            UserResourceKind,
            userId,
            scope.StreamKey,
            RealtimeStreamClock.Next(now),
            UnreadChangedEvent,
            UnreadPayloadContract,
            Serialize(new UnreadFrame(spaceId, unread, mentions)));
    }

    /// <summary>
    /// The payload with its content left out when the frame would exceed <see cref="MaxPayloadBytes"/>. A payload that
    /// is still too large without content cannot happen (the other fields are bounded), so it is sent as it is.
    /// </summary>
    public static JsonElement Fit(MessageFrame frame)
    {
        var full = Serialize(frame);
        if (frame.Content is null || Encoding.UTF8.GetByteCount(full.GetRawText()) <= MaxPayloadBytes)
            return full;

        return Serialize(frame with { Content = null, ContentOmitted = true });
    }

    private static RealtimeResourceChangedV1 Channel(
        Guid eventId, Guid? accountId, Guid? workspaceId, Guid spaceId, Guid? actorUserId,
        Guid correlationId, Guid? causationId, DateTimeOffset occurredAt, string changeKind, JsonElement payload, DateTimeOffset now)
    {
        var scope = new RealtimeScope(RealtimeScopeKind.Channel, spaceId);
        return new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            accountId,
            workspaceId,
            actorUserId,
            correlationId != Guid.Empty ? correlationId : Guid.NewGuid(),
            causationId ?? eventId,
            occurredAt,
            ChannelTopicNamespace,
            ChannelResourceKind,
            spaceId,
            scope.StreamKey,
            RealtimeStreamClock.Next(now),
            changeKind,
            MessagePayloadContract,
            payload);
    }

    private static JsonElement Serialize<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
}
