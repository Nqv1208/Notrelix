namespace Notrelix.API.Contracts.Chat.Requests;

/// <summary>A new channel. A private channel is seen by its members only. Idempotency-Key makes a retry return the same channel.</summary>
public record CreateChannelRequest(string Name, string? Topic, bool IsPrivate, Guid? GroupId);

/// <summary>The new name and topic, with the version the editor read. A stale version is refused with 412.</summary>
public record UpdateChannelRequest(string Name, string? Topic, long? ExpectedVersion);

/// <summary>Archives (true) or unarchives (false) a channel, with the version the client read.</summary>
public record ArchiveChannelRequest(bool? Archived, long? ExpectedVersion);

/// <summary>One channel's sidebar place, with the version the client read for that channel.</summary>
public record ChannelPlacementRequest(Guid ChannelId, Guid? GroupId, double Position, long? ExpectedVersion);

/// <summary>The sidebar order of the channels moved. A stale version of any of them refuses the whole batch with 412.</summary>
public record ReorderChannelsRequest(IReadOnlyList<ChannelPlacementRequest>? Placements);

/// <summary>Workspace members to add to a channel.</summary>
public record AddChannelMembersRequest(IReadOnlyList<Guid>? UserIds);

/// <summary>A new sidebar category. A missing position puts it last.</summary>
public record CreateChannelGroupRequest(string Name, double? Position);

/// <summary>The category's new name and position, with the version the client read. A stale version is 412.</summary>
public record UpdateChannelGroupRequest(string Name, double? Position, long? ExpectedVersion);

/// <summary>
/// A message to a space. ClientMessageId is the sender's key for this message (unique per space and author; a retry
/// with the same one returns the stored message). Idempotency-Key is the transport key of the request, a separate value.
/// </summary>
public record SendMessageRequest(
    string? Content,
    string? ClientMessageId,
    Guid? ReplyToId,
    IReadOnlyList<MentionRequest>? Mentions = null,
    bool? ConfirmBroadcast = null);

/// <summary>
/// One mention in a message's text (UC-CHT-G01, G03, G04). Kind is user, channel-link, broadcast-channel or broadcast-here. Start and
/// Length count UTF-16 characters of the content sent; the text covered starts with @ for a person, # for a channel link, and is
/// @channel or @here for a broadcast. A person is named by userId, a channel by spaceId. The server checks every mention against
/// the channel; one it will not store is shown as text and reported in mentionNotices.
/// </summary>
public record MentionRequest(string? Kind, int? Start, int? Length, Guid? UserId, Guid? SpaceId)
{
    public Notrelix.Application.Features.Chat.Mentions.DTOs.MentionInput ToInput() => new(Kind, Start ?? -1, Length ?? 0, UserId, SpaceId);
}

/// <summary>The new text, with the version the editor read, and the mentions of the new text. A stale version is refused with 412.</summary>
public record EditMessageRequest(string? Content, long? ExpectedVersion, IReadOnlyList<MentionRequest>? Mentions = null, bool? ConfirmBroadcast = null);

/// <summary>The sequence the person has read up to in a space (UC-CHT-D02). A lower sequence than the boundary changes nothing.</summary>
public record MarkSpaceReadRequest(long? UpToSequence);

/// <summary>
/// How the person is notified in a space: all, mentions or none (UC-CHT-D06). With none, an optional until time makes it a
/// timed mute (UC-CHT-D05). An until time with another level is refused.
/// </summary>
public record SetNotificationLevelRequest(string? Level, DateTimeOffset? MutedUntil);

/// <summary>
/// A reply in a thread. ClientMessageId is the sender's key for this reply (unique per space and author). AlsoSendToChannel
/// shows the reply in the channel's main flow too (UC-CHT-F01).
/// </summary>
public record ReplyInThreadRequest(
    string? Content,
    string? ClientMessageId,
    bool? AlsoSendToChannel,
    IReadOnlyList<MentionRequest>? Mentions = null,
    bool? ConfirmBroadcast = null);

/// <summary>The caller's follow state on a thread. following false removes the follow; muted with following silences it (UC-CHT-F04).</summary>
public record FollowThreadRequest(bool? Following, bool? Muted);

/// <summary>The thread sequence the caller has read up to (UC-CHT-F08). A lower value than the pointer changes nothing.</summary>
public record MarkThreadReadRequest(long? UpToReplySequence);

/// <summary>
/// A conversation to start. Kind is direct (with userId, the other person) or group (with userIds, two or more others, and an
/// optional name). Idempotency-Key makes a retry return the same conversation.
/// </summary>
public record StartConversationRequest(string? Kind, Guid? UserId, IReadOnlyList<Guid>? UserIds, string? Name);

/// <summary>Workspace members to add to a group conversation.</summary>
public record AddConversationParticipantsRequest(IReadOnlyList<Guid>? UserIds);

/// <summary>Hides (true) or shows (false) a conversation in the caller's own list.</summary>
public record SetConversationVisibilityRequest(bool? Hidden);

/// <summary>Blocks (true) or unblocks (false) a person of the Workspace for the caller (UC-CHT-E06).</summary>
public record SetPersonBlockRequest(Guid? UserId, bool? Blocked);

/// <summary>One override of a channel: subject is manager, member or guest; action is post, react, attach, pin, broadcast or invite; effect is allow or deny.</summary>
public record ChannelPermissionEntryRequest(string? Subject, string? Action, string? Effect);

/// <summary>The whole set of overrides of a channel, with the version the editor read. Sending fewer entries removes the rest. A stale version is 412.</summary>
public record SetChannelPermissionsRequest(IReadOnlyList<ChannelPermissionEntryRequest>? Entries, long? ExpectedVersion);

/// <summary>A moderator's removal of another person's message: the reason (1 to 500 characters) and the version the moderator read. A stale version is 412.</summary>
public record ModerateMessageRequest(string? Reason, long? ExpectedVersion);
