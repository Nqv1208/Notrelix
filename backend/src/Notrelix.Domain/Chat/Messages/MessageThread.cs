using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// The thread of one root message (CHT-013, UC-CHT-F01, F05). Keyed by the root message id, so a root has at most one
/// thread. The reply count is the thread's own sequence: every reply takes the next value under the space's sequence
/// lock, so it is gap-free and never reused. The thread is open until it has been inactive for
/// <see cref="ChatLimits.ThreadInactivityDays"/> days; a reply reopens it.
/// </summary>
public sealed class MessageThread : IWorkspaceScoped
{
    public Guid RootMessageId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }

    /// <summary>The number of replies stored so far, which is also the sequence of the last reply.</summary>
    public long ReplyCount { get; private set; }

    public DateTimeOffset LastReplyAt { get; private set; }

    /// <summary>Set by the archive sweep (UC-CHT-F05). Null while open. The derived rule <see cref="IsArchivedAt"/> also covers inactive threads.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    private MessageThread() { }

    /// <summary>A thread for a root message that has no replies yet.</summary>
    public static MessageThread Open(Guid accountId, Guid workspaceId, Guid spaceId, Guid rootMessageId, DateTimeOffset at)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(rootMessageId);

        return new MessageThread
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            RootMessageId = rootMessageId,
            ReplyCount = 0,
            LastReplyAt = at,
        };
    }

    /// <summary>
    /// Records one reply: the count goes up, the last reply moves to the reply's time and the thread is open again.
    /// Returns the reply's thread sequence.
    /// </summary>
    public long RecordReply(DateTimeOffset at)
    {
        ReplyCount = checked(ReplyCount + 1);
        LastReplyAt = at;
        ArchivedAt = null;
        return ReplyCount;
    }

    /// <summary>True when the thread is archived at <paramref name="now"/>: archived by the sweep, or inactive for the configured period.</summary>
    public bool IsArchivedAt(DateTimeOffset now) =>
        ArchivedAt is not null || now - LastReplyAt >= TimeSpan.FromDays(ChatLimits.ThreadInactivityDays);
}
