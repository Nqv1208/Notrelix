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
/// A snooze is evaluated against the clock at read time: there is no scheduler that wakes an entry.
/// </summary>
public sealed class PostgresNotificationInbox : INotificationInbox
{
    private readonly INotificationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public PostgresNotificationInbox(INotificationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<IReadOnlyList<InboxUnreadByWorkspace>> UnreadByWorkspaceAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        return await _context.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.RecipientUserId == userId
                        && r.ReadAt == null
                        && r.ArchivedAt == null
                        && r.DismissedAt == null
                        && r.DoneAt == null
                        && (r.SnoozedUntil == null || r.SnoozedUntil <= now))
            .GroupBy(r => r.WorkspaceId)
            .Select(g => new InboxUnreadByWorkspace(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnreadMentionNotification>> RecentUnreadMentionsAsync(
        Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        return await (
                from recipient in _context.NotificationRecipients.AsNoTracking()
                join item in _context.NotificationItems.AsNoTracking() on recipient.NotificationId equals item.Id
                where recipient.WorkspaceId == workspaceId
                      && recipient.RecipientUserId == userId
                      && recipient.ReadAt == null
                      && recipient.ArchivedAt == null
                      && recipient.DismissedAt == null
                      && recipient.DoneAt == null
                      && (recipient.SnoozedUntil == null || recipient.SnoozedUntil <= now)
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
        InboxView view,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
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

        query = view switch
        {
            InboxView.Done => query.Where(x => x.recipient.DoneAt != null),
            InboxView.Snoozed => query.Where(x => x.recipient.DoneAt == null && x.recipient.SnoozedUntil > now),
            InboxView.Unread => query.Where(x => x.recipient.DoneAt == null
                                                 && (x.recipient.SnoozedUntil == null || x.recipient.SnoozedUntil <= now)
                                                 && x.recipient.ReadAt == null),
            _ => query.Where(x => x.recipient.DoneAt == null
                                  && (x.recipient.SnoozedUntil == null || x.recipient.SnoozedUntil <= now))
        };

        if (beforeCreatedAt is { } at && beforeId is { } id)
        {
            query = query.Where(x => x.item.CreatedAt < at || (x.item.CreatedAt == at && x.item.Id.CompareTo(id) < 0));
        }

        var mention = NotificationTypes.MentionProducedType;
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
                x.recipient.ReadAt == null,
                x.item.NotificationType == mention && x.recipient.RepliedAt == null && x.recipient.DoneAt == null,
                _context.NotificationThreadMutes.Any(m => m.UserId == userId
                                                          && m.ResourceKind == x.item.ResourceKind
                                                          && m.ResourceId == x.item.ResourceId),
                x.recipient.SnoozedUntil > now ? x.recipient.SnoozedUntil : null))
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

    public async Task<InboxThread?> FindThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        return await (
                from recipient in _context.NotificationRecipients.AsNoTracking()
                join item in _context.NotificationItems.AsNoTracking() on recipient.NotificationId equals item.Id
                where recipient.WorkspaceId == workspaceId
                      && recipient.RecipientUserId == userId
                      && recipient.NotificationId == notificationId
                select new InboxThread(item.Id, item.AccountId, item.ResourceKind, item.ResourceId, item.NotificationType))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> MarkDoneAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var changed = await OwnEntry(workspaceId, userId, notificationId)
            .Where(r => r.DoneAt == null)
            .ExecuteUpdateAsync(calls => calls
                .SetProperty(r => r.DoneAt, (DateTimeOffset?)at)
                .SetProperty(r => r.SnoozedUntil, (DateTimeOffset?)null)
                .SetProperty(r => r.Status, RecipientStatus.Read)
                .SetProperty(r => r.ReadAt, r => r.ReadAt ?? at)
                .SetProperty(r => r.SeenAt, r => r.SeenAt ?? at)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)at), cancellationToken);
        return changed > 0;
    }

    public async Task<bool> ReopenAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var changed = await OwnEntry(workspaceId, userId, notificationId)
            .Where(r => r.DoneAt != null || r.SnoozedUntil != null)
            .ExecuteUpdateAsync(calls => calls
                .SetProperty(r => r.DoneAt, (DateTimeOffset?)null)
                .SetProperty(r => r.SnoozedUntil, (DateTimeOffset?)null)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)at), cancellationToken);
        return changed > 0;
    }

    public async Task<bool> SnoozeAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset until, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var changed = await OwnEntry(workspaceId, userId, notificationId)
            .ExecuteUpdateAsync(calls => calls
                .SetProperty(r => r.SnoozedUntil, (DateTimeOffset?)until)
                .SetProperty(r => r.DoneAt, (DateTimeOffset?)null)
                .SetProperty(r => r.Status, RecipientStatus.Unread)
                .SetProperty(r => r.ReadAt, (DateTimeOffset?)null)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)at), cancellationToken);
        return changed > 0;
    }

    public async Task<bool> MarkRepliedAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var changed = await OwnEntry(workspaceId, userId, notificationId)
            .Where(r => r.RepliedAt == null)
            .ExecuteUpdateAsync(calls => calls
                .SetProperty(r => r.RepliedAt, (DateTimeOffset?)at)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)at), cancellationToken);
        return changed > 0;
    }

    public async Task<bool> MuteThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var thread = await FindThreadAsync(workspaceId, userId, notificationId, cancellationToken);
        if (thread is not { ResourceKind: { } kind, ResourceId: { } resourceId }) return false;

        // Tracked, not saved here: the request's own unit of work is the only commit owner. The unique
        // (member, thread) index backs the existence check.
        var already = await _context.NotificationThreadMutes.AnyAsync(
            m => m.UserId == userId && m.ResourceKind == kind && m.ResourceId == resourceId, cancellationToken);
        if (!already)
        {
            _context.NotificationThreadMutes.Add(
                NotificationThreadMuteRecord.Create(thread.AccountId, workspaceId, userId, kind, resourceId, at));
        }

        return true;
    }

    public async Task<bool> UnmuteThreadAsync(
        Guid workspaceId, Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var thread = await FindThreadAsync(workspaceId, userId, notificationId, cancellationToken);
        if (thread is not { ResourceKind: { } kind, ResourceId: { } resourceId }) return false;

        await _context.NotificationThreadMutes
            .Where(m => m.UserId == userId && m.ResourceKind == kind && m.ResourceId == resourceId)
            .ExecuteDeleteAsync(cancellationToken);
        return true;
    }

    private IQueryable<NotificationRecipientRecord> OwnEntry(Guid workspaceId, Guid userId, Guid notificationId) =>
        _context.NotificationRecipients.Where(r => r.WorkspaceId == workspaceId
                                                   && r.RecipientUserId == userId
                                                   && r.NotificationId == notificationId
                                                   && r.ArchivedAt == null
                                                   && r.DismissedAt == null);

    /// <summary>The person's unread entries that are in the inbox now: not done and not snoozed.</summary>
    private IQueryable<NotificationRecipientRecord> UnreadOf(Guid workspaceId, Guid userId)
    {
        var now = _clock.UtcNow;
        return _context.NotificationRecipients.Where(r => r.WorkspaceId == workspaceId
                                                          && r.RecipientUserId == userId
                                                          && r.ReadAt == null
                                                          && r.ArchivedAt == null
                                                          && r.DismissedAt == null
                                                          && r.DoneAt == null
                                                          && (r.SnoozedUntil == null || r.SnoozedUntil <= now));
    }

    private static Expression<Func<SetPropertyCalls<NotificationRecipientRecord>, SetPropertyCalls<NotificationRecipientRecord>>> MarkRead(
        DateTimeOffset readAt) =>
        calls => calls
            .SetProperty(r => r.Status, RecipientStatus.Read)
            .SetProperty(r => r.ReadAt, (DateTimeOffset?)readAt)
            .SetProperty(r => r.SeenAt, r => r.SeenAt ?? readAt)
            .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)readAt);
}
