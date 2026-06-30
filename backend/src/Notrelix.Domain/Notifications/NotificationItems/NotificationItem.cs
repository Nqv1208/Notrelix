using Notrelix.Domain.Common;
using Notrelix.Domain.Notifications.NotificationItems.Events;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Notifications.NotificationItems;

public class NotificationItem : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string? DeduplicationKey { get; private set; }
    public string SourceContext { get; private set; } = null!;
    public Guid? SourceEventId { get; private set; }
    public Guid? SourceMessageId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public NotificationSeverity Severity { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public JsonValue DataJson { get; private set; } = JsonValue.EmptyObject();
    public NotificationItemStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private NotificationItem() : base() { }

    public static NotificationItem Create(
        Guid workspaceId,
        string sourceContext,
        string notificationType,
        NotificationSeverity severity,
        string title,
        DateTimeOffset createdAt,
        Guid? actorUserId = null,
        Guid? sourceEventId = null,
        Guid? sourceMessageId = null,
        string? subjectType = null,
        Guid? subjectId = null,
        string? resourceType = null,
        Guid? resourceId = null,
        string? body = null,
        string? actionUrl = null,
        JsonValue? dataJson = null,
        string? deduplicationKey = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(sourceContext);
        Guard.NotNullOrWhiteSpace(notificationType);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 320);

        if (subjectType is not null && subjectId is null)
            throw new BusinessRuleException("SubjectId is required when SubjectType is provided.");
        if (subjectType is null && subjectId.HasValue)
            throw new BusinessRuleException("SubjectType is required when SubjectId is provided.");
        if (resourceType is not null && resourceId is null)
            throw new BusinessRuleException("ResourceId is required when ResourceType is provided.");
        if (resourceType is null && resourceId.HasValue)
            throw new BusinessRuleException("ResourceType is required when ResourceId is provided.");

        var item = new NotificationItem
        {
            WorkspaceId = workspaceId,
            DeduplicationKey = deduplicationKey,
            SourceContext = sourceContext.Trim(),
            SourceEventId = sourceEventId,
            SourceMessageId = sourceMessageId,
            ActorUserId = actorUserId,
            NotificationType = notificationType.Trim(),
            Severity = severity,
            SubjectType = subjectType,
            SubjectId = subjectId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Title = title.Trim(),
            Body = body?.Trim(),
            ActionUrl = actionUrl,
            DataJson = dataJson ?? JsonValue.EmptyObject(),
            Status = NotificationItemStatus.Active,
            ExpiresAt = expiresAt
        };

        item.SetAuditOnCreate(actorUserId, createdAt);
        item.AddDomainEvent(new NotificationItemCreatedDomainEvent(
            workspaceId, item.Id, notificationType, title, actorUserId, createdAt));
        return item;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status != NotificationItemStatus.Active) return;
        Status = NotificationItemStatus.Cancelled;
        SetAuditOnUpdate(ActorUserId, cancelledAt);
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
    }
}
