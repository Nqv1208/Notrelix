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
    bool Unread);

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
    /// dismissed notifications are not part of the inbox. <paramref name="limit"/> bounds the page.
    /// </summary>
    Task<IReadOnlyList<InboxNotification>> ListAsync(
        Guid workspaceId,
        Guid userId,
        bool unreadOnly,
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
}
