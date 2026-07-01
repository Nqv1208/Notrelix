using Notrelix.Application.Events.Collaboration;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class MentionCreatedNotificationConsumer : IConsumer<MentionCreatedIntegrationEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MentionCreatedNotificationConsumer> _logger;

    public MentionCreatedNotificationConsumer(
        ApplicationDbContext context,
        ILogger<MentionCreatedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MentionCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var now = DateTimeOffset.UtcNow;

        if (msg.WorkspaceId is null)
        {
            _logger.LogWarning("MentionCreated event {MentionId} has no WorkspaceId, skipping notification", msg.MentionId);
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

        var notificationItem = NotificationItemRecord.Create(
            accountId: Guid.Empty,
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "collaboration",
            notificationType: "mention.created",
            severity: NotificationSeverity.Info,
            title: "You were mentioned",
            createdAt: now,
            actorUserId: msg.MentionedByUserId,
            sourceEventId: msg.EventId,
            subjectType: "Mention",
            subjectId: msg.MentionId,
            resourceType: msg.TargetType,
            resourceId: msg.TargetId,
            body: $"You were mentioned in a {msg.TargetType.ToLowerInvariant()}.",
            deduplicationKey: deduplicationKey);

        _context.NotificationItems.Add(notificationItem);

        var recipient = NotificationRecipientRecord.Create(
            accountId: Guid.Empty,
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
}
