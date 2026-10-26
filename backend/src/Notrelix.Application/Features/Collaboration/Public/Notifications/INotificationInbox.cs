namespace Notrelix.Application.Features.Collaboration.Public.Notifications;

/// <summary>Unread notifications one person holds in one Workspace.</summary>
public sealed record InboxUnreadByWorkspace(Guid WorkspaceId, int Unread);

/// <summary>One unread mention notification addressed to the reader.</summary>
public sealed record UnreadMentionNotification(
    Guid NotificationId,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt,
    string? Body,
    string? ResourceKind,
    Guid? ResourceId);

/// <summary>One notification of the reader's own inbox with its read state.</summary>
public sealed record InboxNotification(
    Guid NotificationId,
    string NotificationType,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt,
    string Title,
    string? Body,
    string? ResourceKind,
    Guid? ResourceId,
    bool Unread,
    bool NeedsReply = false,
    bool Muted = false,
    DateTimeOffset? SnoozedUntil = null);

/// <summary>Which of the person's entries a listing shows.</summary>
public enum InboxView
{
    /// <summary>The default list: not done, not snoozed.</summary>
    Inbox,

    /// <summary>The default list narrowed to unread entries.</summary>
    Unread,

    /// <summary>Entries the person marked done.</summary>
    Done,

    /// <summary>Entries hidden until a later time.</summary>
    Snoozed
}

/// <summary>The thread a notification belongs to, which a reply or a mute addresses.</summary>
public sealed record InboxThread(Guid NotificationId, Guid AccountId, string? ResourceKind, Guid? ResourceId, string NotificationType);

/// <summary>
/// Collaboration-owned read of the reader's own notification inbox. A notification belongs to its
/// recipient: every read is addressed by the recipient and bounded, never by a Workspace-wide scan,
/// and the database enforces the same recipient ownership below the Application. A notification is
/// unread until its recipient reads, archives or dismisses it.
/// </summary>
public interface INotificationInbox
{
    /// <summary>Unread notification counts of the person per Workspace; Workspaces with none are omitted.</summary>
    Task<IReadOnlyList<InboxUnreadByWorkspace>> UnreadByWorkspaceAsync(
        Guid userId, CancellationToken cancellationToken);

    /// <summary>The newest unread mention notifications of the person in one Workspace, newest first.</summary>
    Task<IReadOnlyList<UnreadMentionNotification>> RecentUnreadMentionsAsync(
        Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// A page of the person's own notifications in one Workspace, newest first, strictly after the cursor
    /// (<paramref name="beforeCreatedAt"/>, <paramref name="beforeId"/>) when both are given. Archived and
    /// dismissed notifications are not part of the inbox; done and still-snoozed ones are shown only by
    /// their own view. A snooze that has elapsed is evaluated at read time: the entry is back, unread.
    /// <paramref name="limit"/> bounds the page.
    /// </summary>
    Task<IReadOnlyList<InboxNotification>> ListAsync(
        Guid workspaceId,
        Guid userId,
        InboxView view,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>Marks one of the person's own unread notifications read; false when it is not theirs or not unread.</summary>
    Task<bool> MarkReadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken);

    /// <summary>Marks every unread notification of the person in one Workspace read; answers how many changed.</summary>
    Task<int> MarkAllReadAsync(
        Guid workspaceId, Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken);

    /// <summary>The thread of one of the person's own notifications; null when it is not theirs.</summary>
    Task<InboxThread?> FindThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, CancellationToken cancellationToken);

    /// <summary>Marks one of the person's own entries done (and read); false when it is not theirs or already done.</summary>
    Task<bool> MarkDoneAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Takes an entry back into the inbox: not done, not snoozed.</summary>
    Task<bool> ReopenAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Hides one of the person's own entries until <paramref name="until"/>, when it returns unread.</summary>
    Task<bool> SnoozeAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset until, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Records that the person replied on the thread of the entry, which ends "needs your reply".</summary>
    Task<bool> MarkRepliedAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Mutes the thread of the entry for the person; a thread already muted stays muted. False when the entry is not theirs or has no thread.</summary>
    Task<bool> MuteThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Lifts the person's mute of the thread of the entry.</summary>
    Task<bool> UnmuteThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, CancellationToken cancellationToken);
}
