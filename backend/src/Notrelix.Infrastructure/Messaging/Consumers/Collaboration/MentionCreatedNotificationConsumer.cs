using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Features.Collaboration.Notifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

[OrderingNotApplicable("Each mention creates one notification guarded by a per-mention-and-user deduplication key; notifications are independent and never supersede each other.")]
public sealed class MentionCreatedNotificationConsumer : IConsumer<MentionCreatedIntegrationEvent>
{
    private readonly INotificationDbContext _context;
    private readonly ILogger<MentionCreatedNotificationConsumer> _logger;

    public MentionCreatedNotificationConsumer(
        INotificationDbContext context,
        ILogger<MentionCreatedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MentionCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var now = DateTimeOffset.UtcNow;

        if (msg.WorkspaceId is null || msg.AccountId is null || msg.AccountId == Guid.Empty)
        {
            _logger.LogWarning(
                "MentionCreated event {MentionId} has no authoritative AccountId/WorkspaceId envelope, skipping notification",
                msg.MentionId);
            return;
        }

        var deduplicationKey = $"mention-created:{msg.MentionId}:{msg.MentionedUserId}";

        var existing = await _context.NotificationItems
            .AsNoTracking()
            .Where(n => n.DeduplicationKey == deduplicationKey)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug("Notification already exists for mention {MentionId}, skipping", msg.MentionId);
            return;
        }

        var preferenceType = NotificationTypes.FromProducedType(NotificationTypes.MentionProducedType)!;
        if (!await IsInAppEnabledAsync(msg.AccountId.Value, msg.WorkspaceId.Value, msg.MentionedUserId, preferenceType, context.CancellationToken))
        {
            _logger.LogInformation(
                "Mention {MentionId} -> user {UserId} not notified in-app: the user turned the {Type} in-app preference off in workspace {WorkspaceId}",
                msg.MentionId, msg.MentionedUserId, preferenceType, msg.WorkspaceId);
            return;
        }

        if (await _context.NotificationThreadMutes.AsNoTracking().AnyAsync(
                m => m.UserId == msg.MentionedUserId && m.ResourceKind == msg.TargetType && m.ResourceId == msg.TargetId,
                context.CancellationToken))
        {
            _logger.LogInformation(
                "Mention {MentionId} -> user {UserId} not notified: the user muted the thread in workspace {WorkspaceId}",
                msg.MentionId, msg.MentionedUserId, msg.WorkspaceId);
            return;
        }

        var notificationItem = NotificationItemRecord.Create(
            accountId: msg.AccountId.Value,
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "collaboration",
            notificationType: NotificationTypes.MentionProducedType,
            severity: NotificationSeverity.Info,
            title: "You were mentioned",
            createdAt: now,
            actorUserId: msg.MentionedByUserId,
            sourceEventId: msg.EventId,
            subjectType: "Mention",
            subjectId: msg.MentionId,
            resourceType: msg.TargetType,
            resourceId: msg.TargetId,
            body: NotificationExcerpt.From(msg.Excerpt) ?? $"You were mentioned in a {msg.TargetType.ToLowerInvariant()}.",
            deduplicationKey: deduplicationKey);

        _context.NotificationItems.Add(notificationItem);

        var recipient = NotificationRecipientRecord.Create(
            accountId: msg.AccountId.Value,
            notificationId: notificationItem.Id,
            workspaceId: msg.WorkspaceId.Value,
            recipientUserId: msg.MentionedUserId,
            createdAt: now);

        _context.NotificationRecipients.Add(recipient);

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Created notification for mention {MentionId} -> user {UserId} in workspace {WorkspaceId}",
            msg.MentionId, msg.MentionedUserId, msg.WorkspaceId);
    }

    /// <summary>
    /// The mentioned person's own in-app choice for this type in this Workspace, else the default (on). The
    /// consumer reads under the tenant restored from the event envelope, and addresses the set by that
    /// (Account, Workspace) and the recipient, never by an ambient request user.
    /// </summary>
    private async Task<bool> IsInAppEnabledAsync(Guid accountId, Guid workspaceId, Guid userId, string type, CancellationToken ct)
    {
        var set = await _context.NotificationPreferenceSets
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.AccountId == accountId && s.WorkspaceId == workspaceId && s.UserId == userId, ct);

        return set?.IsEnabled(type, NotificationChannels.InApp)
            ?? NotificationPreferenceDefaults.IsEnabled(type, NotificationChannels.InApp);
    }
}
