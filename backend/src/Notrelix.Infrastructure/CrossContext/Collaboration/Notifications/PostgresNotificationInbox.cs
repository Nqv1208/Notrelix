using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;

/// <summary>
/// Collaboration-owned implementation of the notification inbox read. Runs under the application login,
/// whose row-level policy lets a person read only their own recipient rows and the items those rows point to.
/// </summary>
public sealed class PostgresNotificationInbox : INotificationInbox
{
    private readonly INotificationDbContext _context;

    public PostgresNotificationInbox(INotificationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InboxUnreadByWorkspace>> UnreadByWorkspaceAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        return await _context.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.RecipientUserId == userId
                        && r.ReadAt == null
                        && r.ArchivedAt == null
                        && r.DismissedAt == null)
            .GroupBy(r => r.WorkspaceId)
            .Select(g => new InboxUnreadByWorkspace(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnreadMentionNotification>> RecentUnreadMentionsAsync(
        Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken)
    {
        return await (
                from recipient in _context.NotificationRecipients.AsNoTracking()
                join item in _context.NotificationItems.AsNoTracking() on recipient.NotificationId equals item.Id
                where recipient.WorkspaceId == workspaceId
                      && recipient.RecipientUserId == userId
                      && recipient.ReadAt == null
                      && recipient.ArchivedAt == null
                      && recipient.DismissedAt == null
                      && item.NotificationType == NotificationTypes.MentionProducedType
                      && item.Status == NotificationItemStatus.Active
                      && item.DeletedAt == null
                orderby item.CreatedAt descending, item.Id descending
                select new UnreadMentionNotification(
                    item.Id, item.ActorUserId, item.CreatedAt, item.Body, item.ResourceKind, item.ResourceId))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
