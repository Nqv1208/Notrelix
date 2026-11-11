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
