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
}
