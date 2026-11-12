using System.Text;
using System.Text.Json;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Attachments.Services;
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
    public const string SpaceChangedEvent = "chat.space.changed";
    public const string ReactionChangedEvent = "chat.reaction.changed";
    public const string AttachmentChangedEvent = "chat.message.attachment.changed";

    public const string MessagePayloadContract = "chat.message.v1";
    public const string UnreadPayloadContract = "chat.unread.v1";
    public const string SpaceChangedPayloadContract = "chat.space.v1";
    public const string ReactionPayloadContract = "chat.reaction.v1";
    public const string AttachmentPayloadContract = "chat.attachment.v1";

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

    /// <summary>
    /// The message payload of the channel stream (contract <see cref="MessagePayloadContract"/>). ThreadRootId is set for a
    /// thread reply (UC-CHT-F01): the frame still goes to the channel stream, and ThreadSequence lets a client that holds the
    /// root count its replies without a round trip (the count is the highest sequence it has seen).
    /// </summary>
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
        long Version,
        Guid? ThreadRootId = null,
        long? ThreadSequence = null);

    /// <summary>The per-user unread payload (contract <see cref="UnreadPayloadContract"/>).</summary>
    public sealed record UnreadFrame(Guid SpaceId, int Unread, int Mentions);

    public static RealtimeResourceChangedV1 ForCreated(ChatMessageCreatedIntegrationEvent e, DateTimeOffset now)
    {
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, e.Kind, e.Content, ContentOmitted: false, Deleted: false,
            CreatedAt: e.OccurredAt, EditedAt: null, ReplyToId: e.ReplyToId, Version: e.Version,
            ThreadRootId: e.ThreadRootId, ThreadSequence: e.ThreadSequence);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageCreatedEvent, Fit(frame), now);
    }

    public static RealtimeResourceChangedV1 ForUpdated(ChatMessageUpdatedIntegrationEvent e, DateTimeOffset now)
    {
        // Only a user message is edited (a system message is immutable), so the kind is known.
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, MessageKindWire.UserKind, e.Content, ContentOmitted: false, Deleted: false,
            CreatedAt: null, EditedAt: e.OccurredAt, ReplyToId: null, Version: e.Version, ThreadRootId: e.ThreadRootId);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageUpdatedEvent, Fit(frame), now);
    }

    public static RealtimeResourceChangedV1 ForDeleted(ChatMessageDeletedIntegrationEvent e, DateTimeOffset now)
    {
        // A tombstone keeps its id, sequence, author and version; the content is gone (null).
        var frame = new MessageFrame(
            e.MessageId, e.Sequence, e.AuthorId, MessageKindWire.UserKind, Content: null, ContentOmitted: false, Deleted: true,
            CreatedAt: null, EditedAt: null, ReplyToId: null, Version: e.Version, ThreadRootId: e.ThreadRootId);
        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, MessageDeletedEvent, Serialize(frame), now);
    }

    /// <summary>
    /// A pin changed in a space (UC-CHT-F06, F07): the change ("pinned" or "unpinned") and the message. It carries no content;
    /// a reader that needs the pinned message refetches the pins over REST.
    /// </summary>
    public static RealtimeResourceChangedV1 ForSpaceChanged(ChatSpaceChangedIntegrationEvent e, DateTimeOffset now)
        => Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, SpaceChangedEvent, Serialize(new SpaceChangedFrame(e.ChangeKind, e.MessageId)), now, SpaceChangedPayloadContract);

    /// <summary>The pin payload (contract <see cref="SpaceChangedPayloadContract"/>).</summary>
    public sealed record SpaceChangedFrame(string Change, Guid MessageId);

    /// <summary>
    /// A reaction changed on a message of a space (UC-CHT-G06, G07): the message, the kind, the change ("added" or "removed") and who
    /// changed it. No counts and no content: a reader applies the change to the summary it holds, or refetches the message's
    /// reactions over REST (the frame is an advisory, the REST summary is the authority). Contract <see cref="ReactionPayloadContract"/>.
    /// </summary>
    public static RealtimeResourceChangedV1 ForReactionChanged(ChatReactionChangedIntegrationEvent e, DateTimeOffset now)
        => Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, ReactionChangedEvent, Serialize(new ReactionChangedFrame(e.MessageId, e.Kind, e.ChangeKind, e.ActorUserId ?? Guid.Empty)), now, ReactionPayloadContract);

    /// <summary>The reaction payload (contract <see cref="ReactionPayloadContract"/>).</summary>
    public sealed record ReactionChangedFrame(Guid MessageId, string Kind, string Change, Guid ActorId);

    /// <summary>
    /// A file was added to or removed from a message of a space (UC-CHT-G08). An added file carries its metadata and the authorized
    /// download route (no bytes, no URL that works without a permission check); a removed file carries only its id. A reader applies
    /// the change by file id, so a replay is harmless. A file name so long that the frame would not fit is left out (DetailsOmitted)
    /// and the reader refetches the message's files over REST. Contract <see cref="AttachmentPayloadContract"/>.
    /// </summary>
    public static RealtimeResourceChangedV1 ForAttachmentChanged(ChatMessageAttachmentChangedIntegrationEvent e, DateTimeOffset now)
    {
        var added = e.ChangeKind == "added";
        var frame = new AttachmentChangedFrame(
            e.MessageId, e.ChangeKind, e.AttachmentId,
            added ? e.FileName : null,
            added ? e.SizeBytes : null,
            added ? e.ContentType : null,
            added ? e.UploadedBy : null,
            added ? e.AttachmentCreatedAt : null,
            added && e.WorkspaceId is { } workspaceId ? MessageAttachments.DownloadPath(workspaceId, e.MessageId, e.AttachmentId) : null,
            DetailsOmitted: false);
        var payload = Serialize(frame);
        if (Encoding.UTF8.GetByteCount(payload.GetRawText()) > MaxPayloadBytes)
            payload = Serialize(new AttachmentChangedFrame(e.MessageId, e.ChangeKind, e.AttachmentId, null, null, null, null, null, null, DetailsOmitted: true));

        return Channel(e.EventId, e.AccountId, e.WorkspaceId, e.SpaceId, e.ActorUserId, e.CorrelationId, e.CausationId,
            e.OccurredAt, AttachmentChangedEvent, payload, now, AttachmentPayloadContract);
    }

    /// <summary>The file payload (contract <see cref="AttachmentPayloadContract"/>). Only the fields of an added file are set; a removal has the ids and the change.</summary>
    public sealed record AttachmentChangedFrame(
        Guid MessageId,
        string Change,
        Guid AttachmentId,
        string? FileName,
        long? SizeBytes,
        string? ContentType,
        Guid? UploadedBy,
        DateTimeOffset? CreatedAt,
        string? DownloadPath,
        bool DetailsOmitted);

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
        Guid correlationId, Guid? causationId, DateTimeOffset occurredAt, string eventName, JsonElement payload, DateTimeOffset now,
        string payloadContract = MessagePayloadContract)
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
            eventName,
            payloadContract,
            payload);
    }

    private static JsonElement Serialize<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
}
