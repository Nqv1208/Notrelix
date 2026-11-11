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
public record SendMessageRequest(string? Content, string? ClientMessageId, Guid? ReplyToId);

/// <summary>The new text, with the version the editor read. A stale version is refused with 412.</summary>
public record EditMessageRequest(string? Content, long? ExpectedVersion);

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
public record ReplyInThreadRequest(string? Content, string? ClientMessageId, bool? AlsoSendToChannel);

/// <summary>The caller's follow state on a thread. following false removes the follow; muted with following silences it (UC-CHT-F04).</summary>
public record FollowThreadRequest(bool? Following, bool? Muted);

/// <summary>The thread sequence the caller has read up to (UC-CHT-F08). A lower value than the pointer changes nothing.</summary>
public record MarkThreadReadRequest(long? UpToReplySequence);
