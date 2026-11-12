namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// A person's follow state on one thread (CHT-013, UC-CHT-F04, F08). Keyed by (root message, user). A follower gets the
/// thread's replies as unread; a muted follower gets no notification for them. The read pointer is the highest
/// thread sequence the person has seen and moves forward only (the rule of ReadStates.ReadBoundary).
/// </summary>
public sealed class ThreadFollower : IWorkspaceScoped
{
    public Guid RootMessageId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public bool Muted { get; private set; }

    /// <summary>The highest thread sequence the person has read. Replies above it are unread.</summary>
    public long ReadReplySequence { get; private set; }

    private ThreadFollower() { }

    public static ThreadFollower Follow(Guid accountId, Guid workspaceId, Guid rootMessageId, Guid userId, bool muted, long readReplySequence = 0)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(rootMessageId);
        Guard.NotEmpty(userId);

        return new ThreadFollower
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RootMessageId = rootMessageId,
            UserId = userId,
            Muted = muted,
            ReadReplySequence = Math.Max(0, readReplySequence),
        };
    }

    public void SetMuted(bool muted) => Muted = muted;

    /// <summary>Moves the read pointer forward to the thread sequence the person has now seen. A lower value changes nothing.</summary>
    public void MarkReadThrough(long threadSequence) => ReadReplySequence = Math.Max(ReadReplySequence, threadSequence);

    /// <summary>Unread replies against the thread's reply count. Never negative.</summary>
    public long UnreadReplies(long replyCount) => Math.Max(0, replyCount - ReadReplySequence);
}
