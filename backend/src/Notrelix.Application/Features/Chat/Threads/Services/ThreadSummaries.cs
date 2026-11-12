using Notrelix.Application.Features.Chat.Threads.DTOs;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Threads.Services;

/// <summary>Builds the summary a caller sees for a thread. A missing thread (no replies yet) has a zero count.</summary>
public static class ThreadSummaries
{
    public static ThreadSummaryDto For(
        Guid rootMessageId, Guid spaceId, MessageThread? thread, ThreadFollower? follower, DateTimeOffset now)
    {
        var replyCount = thread?.ReplyCount ?? 0;
        return new ThreadSummaryDto(
            rootMessageId,
            spaceId,
            replyCount,
            thread?.LastReplyAt,
            Archived: thread?.IsArchivedAt(now) ?? false,
            Following: follower is not null,
            Muted: follower?.Muted ?? false,
            UnreadReplies: follower?.UnreadReplies(replyCount) ?? 0);
    }
}
