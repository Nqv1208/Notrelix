using Notrelix.Application.Features.Chat.Messages.DTOs;

namespace Notrelix.Application.Features.Chat.Threads.DTOs;

/// <summary>
/// One thread as the caller sees it: its reply count and last reply, whether it is archived (UC-CHT-F05), and the
/// caller's follow state and unread replies (UC-CHT-F04, F08). A thread the caller does not follow has no unread replies.
/// </summary>
public sealed record ThreadSummaryDto(
    Guid RootMessageId,
    Guid SpaceId,
    long ReplyCount,
    DateTimeOffset? LastReplyAt,
    bool Archived,
    bool Following,
    bool Muted,
    long UnreadReplies);

/// <summary>A page of a thread's replies in thread sequence order, with the root on top (UC-CHT-F03).</summary>
public sealed record ThreadRepliesPageDto(
    MessageDto Root,
    ThreadSummaryDto Thread,
    IReadOnlyList<MessageDto> Items,
    bool HasNewer);

/// <summary>The caller's follow state after a follow or mute request (UC-CHT-F04).</summary>
public sealed record ThreadFollowDto(Guid RootMessageId, bool Following, bool Muted);

/// <summary>The caller's unread replies in a thread after a read request (UC-CHT-F08).</summary>
public sealed record ThreadReadDto(Guid RootMessageId, long UnreadReplies);

/// <summary>One followed thread in the caller's list: its root and its summary (UC-CHT-F08).</summary>
public sealed record FollowedThreadDto(MessageDto Root, ThreadSummaryDto Thread);

/// <summary>A page of followed threads, most recent reply first. NextCursor is null on the last page.</summary>
public sealed record FollowedThreadPageDto(IReadOnlyList<FollowedThreadDto> Items, string? NextCursor);
