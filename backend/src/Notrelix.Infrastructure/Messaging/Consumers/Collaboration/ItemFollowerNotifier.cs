using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Notifications;
using Notrelix.Application.Features.Collaboration.Public.Watchers;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>
/// The follow side of an Item event, shared by the Collaboration consumers of the Work and comment facts:
/// a person who creates or is assigned an Item follows it, and the people who follow it are told in-app
/// about a change. Every write is idempotent by the message identity, so a redelivery adds nothing.
/// </summary>
public sealed class ItemFollowerNotifier
{
    public const string BoardItemKind = "work-management.board-item";
    public const int ExcerptLength = NotificationExcerpt.Length;

    private readonly ICollaborationDbContext _collaboration;
    private readonly INotificationDbContext _notifications;
    private readonly IResourceFollowers _followers;

    public ItemFollowerNotifier(
        ICollaborationDbContext collaboration,
        INotificationDbContext notifications,
        IResourceFollowers followers)
    {
        _collaboration = collaboration;
        _notifications = notifications;
        _followers = followers;
    }

    /// <summary>Makes the person follow the Item unless they already do.</summary>
    public async Task EnsureFollowingAsync(
        Guid accountId, Guid workspaceId, Guid itemId, Guid userId, Guid addedBy, DateTimeOffset now, CancellationToken ct)
    {
        var kind = ResourceKind.Create(BoardItemKind);
        var already = await _collaboration.ResourceWatchers
            .AnyAsync(w => w.UserId == userId && w.Target.Kind == kind && w.Target.ResourceId == itemId, ct);
        if (already) return;

        _collaboration.ResourceWatchers.Add(ResourceWatcher.Create(
            accountId, workspaceId, ResourceRef.Create(kind, itemId, workspaceId), userId, addedBy, now));
        await _notifications.SaveChangesAsync(ct); // the same unit of work as the collaboration set
    }

    /// <summary>
    /// One in-app notification of <paramref name="producedType"/> to each follower of the Item who turned the
    /// matching preference on, except the actor and <paramref name="alsoExcluded"/>.
    /// </summary>
    public async Task NotifyFollowersAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid? actorUserId,
        Guid sourceEventId,
        string producedType,
        string title,
        string? body,
        IReadOnlyCollection<Guid> alsoExcluded,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var followers = await _followers.FollowerIdsAsync(BoardItemKind, itemId, ct);
        var recipients = followers
            .Where(id => id != actorUserId && !alsoExcluded.Contains(id))
            .ToList();
        await NotifyAsync(accountId, workspaceId, itemId, actorUserId, sourceEventId, producedType, title, body, recipients, now, ct);
    }

    /// <summary>One in-app notification to each listed person who turned the matching preference on.</summary>
    public Task NotifyAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid? actorUserId,
        Guid sourceEventId,
        string producedType,
        string title,
        string? body,
        IReadOnlyCollection<Guid> recipients,
        DateTimeOffset now,
        CancellationToken ct) =>
        NotifyAsync(
            accountId, workspaceId, itemId, actorUserId, sourceEventId, sourceEventId.ToString(),
            producedType, title, body, recipients, now, ct);

    /// <summary>
    /// The same, idempotent by <paramref name="deduplicationScope"/> instead of an event: a producer that has no
    /// source event (the daily due-date job) names its own scope, such as the Item and the due date.
    /// <paramref name="sourceEventId"/> is stored on the notification as given and may be null.
    /// </summary>
    public async Task NotifyAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid? actorUserId,
        Guid? sourceEventId,
        string deduplicationScope,
        string producedType,
        string title,
        string? body,
        IReadOnlyCollection<Guid> recipients,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (recipients.Count == 0) return;

        var prefix = $"{producedType}:{deduplicationScope}:";
        var keys = recipients.Select(id => prefix + id).ToList();
        var existing = (await _notifications.NotificationItems
                .AsNoTracking()
                .Where(n => n.DeduplicationKey != null && keys.Contains(n.DeduplicationKey))
                .Select(n => n.DeduplicationKey!)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        // A person who muted the thread hears nothing new from it.
        var mutedBy = (await _notifications.NotificationThreadMutes
                .AsNoTracking()
                .Where(m => m.ResourceKind == BoardItemKind && m.ResourceId == itemId && recipients.Contains(m.UserId))
                .Select(m => m.UserId)
                .ToListAsync(ct))
            .ToHashSet();

        var preferenceType = NotificationTypes.FromProducedType(producedType)!;
        var sets = await _notifications.NotificationPreferenceSets
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.AccountId == accountId && s.WorkspaceId == workspaceId && recipients.Contains(s.UserId))
            .ToListAsync(ct);

        var added = false;
        foreach (var userId in recipients.Distinct())
        {
            var key = prefix + userId;
            if (existing.Contains(key) || mutedBy.Contains(userId)) continue;

            var set = sets.FirstOrDefault(s => s.UserId == userId);
            var enabled = set?.IsEnabled(preferenceType, NotificationChannels.InApp)
                ?? NotificationPreferenceDefaults.IsEnabled(preferenceType, NotificationChannels.InApp);
            if (!enabled) continue;

            var item = NotificationItemRecord.Create(
                accountId: accountId,
                workspaceId: workspaceId,
                sourceContext: "collaboration",
                notificationType: producedType,
                severity: NotificationSeverity.Info,
                title: title,
                createdAt: now,
                actorUserId: actorUserId,
                sourceEventId: sourceEventId,
                subjectType: "BoardItem",
                subjectId: itemId,
                resourceType: BoardItemKind,
                resourceId: itemId,
                body: body,
                deduplicationKey: key);
            _notifications.NotificationItems.Add(item);
            _notifications.NotificationRecipients.Add(NotificationRecipientRecord.Create(
                accountId: accountId,
                notificationId: item.Id,
                workspaceId: workspaceId,
                recipientUserId: userId,
                createdAt: now));
            added = true;
        }

        if (added) await _notifications.SaveChangesAsync(ct);
    }

    /// <summary>
    /// A person who comments on a thread has replied to the mentions they hold there: those notifications no
    /// longer need their reply. Idempotent; only the author's own unanswered mention entries of that thread change.
    /// </summary>
    public async Task MarkMentionsRepliedAsync(
        Guid workspaceId, string threadKind, Guid threadId, Guid authorUserId, DateTimeOffset now, CancellationToken ct)
    {
        var mentionIds = _notifications.NotificationItems
            .Where(n => n.WorkspaceId == workspaceId
                        && n.NotificationType == NotificationTypes.MentionProducedType
                        && n.ResourceKind == threadKind
                        && n.ResourceId == threadId);

        await _notifications.NotificationRecipients
            .Where(r => r.WorkspaceId == workspaceId
                        && r.RecipientUserId == authorUserId
                        && r.RepliedAt == null
                        && mentionIds.Any(n => n.Id == r.NotificationId))
            .ExecuteUpdateAsync(calls => calls
                .SetProperty(r => r.RepliedAt, (DateTimeOffset?)now)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)now), ct);
    }

    /// <summary>The text of a comment as a plain one-line excerpt of at most <see cref="ExcerptLength"/> characters.</summary>
    public static string? Excerpt(string? text) => NotificationExcerpt.From(text);
}
