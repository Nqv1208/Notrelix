using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
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

    public async Task<IReadOnlyList<InboxNotification>> ListAsync(
        Guid workspaceId,
        Guid userId,
        bool unreadOnly,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query =
            from recipient in _context.NotificationRecipients.AsNoTracking()
            join item in _context.NotificationItems.AsNoTracking() on recipient.NotificationId equals item.Id
            where recipient.WorkspaceId == workspaceId
                  && recipient.RecipientUserId == userId
                  && recipient.ArchivedAt == null
                  && recipient.DismissedAt == null
                  && item.Status == NotificationItemStatus.Active
                  && item.DeletedAt == null
            select new { recipient, item };

        if (unreadOnly)
        {
            query = query.Where(x => x.recipient.ReadAt == null);
        }

        if (beforeCreatedAt is { } at && beforeId is { } id)
        {
            query = query.Where(x => x.item.CreatedAt < at || (x.item.CreatedAt == at && x.item.Id.CompareTo(id) < 0));
        }

        return await query
            .OrderByDescending(x => x.item.CreatedAt)
            .ThenByDescending(x => x.item.Id)
            .Take(limit)
            .Select(x => new InboxNotification(
                x.item.Id,
                x.item.NotificationType,
                x.item.ActorUserId,
                x.item.CreatedAt,
                x.item.Title,
                x.item.Body,
                x.item.ResourceKind,
                x.item.ResourceId,
                x.recipient.ReadAt == null))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var changed = await UnreadOf(workspaceId, userId)
            .Where(r => r.NotificationId == notificationId)
            .ExecuteUpdateAsync(MarkRead(readAt), cancellationToken);
        return changed > 0;
    }

    public Task<int> MarkAllReadAsync(
        Guid workspaceId, Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken) =>
        UnreadOf(workspaceId, userId).ExecuteUpdateAsync(MarkRead(readAt), cancellationToken);

    private IQueryable<NotificationRecipientRecord> UnreadOf(Guid workspaceId, Guid userId) =>
        _context.NotificationRecipients.Where(r => r.WorkspaceId == workspaceId
                                                   && r.RecipientUserId == userId
                                                   && r.ReadAt == null
                                                   && r.ArchivedAt == null
                                                   && r.DismissedAt == null);

    private static Expression<Func<SetPropertyCalls<NotificationRecipientRecord>, SetPropertyCalls<NotificationRecipientRecord>>> MarkRead(
        DateTimeOffset readAt) =>
        calls => calls
            .SetProperty(r => r.Status, RecipientStatus.Read)
            .SetProperty(r => r.ReadAt, (DateTimeOffset?)readAt)
            .SetProperty(r => r.SeenAt, r => r.SeenAt ?? readAt)
            .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)readAt);
}
