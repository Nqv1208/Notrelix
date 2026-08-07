using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

public sealed class NotificationItemRecord
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
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
    public string? ResourceKind { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public JsonDocument DataJson { get; private set; } = JsonDocument.Parse("{}");
    public NotificationItemStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? DeleteReason { get; private set; }
    public DateTimeOffset? RestoredAt { get; private set; }
    public Guid? RestoredBy { get; private set; }
    public long Version { get; private set; } = 1;

    private NotificationItemRecord() { }

    public static NotificationItemRecord Create(
        Guid accountId,
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
        JsonDocument? dataJson = null,
        string? deduplicationKey = null,
        DateTimeOffset? expiresAt = null)
    {
        return new NotificationItemRecord
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
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
            ResourceKind = resourceType,
            ResourceId = resourceId,
            Title = title.Trim(),
            Body = body?.Trim(),
            ActionUrl = actionUrl,
            DataJson = dataJson ?? JsonDocument.Parse("{}"),
            Status = NotificationItemStatus.Active,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            CreatedBy = actorUserId
        };
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status != NotificationItemStatus.Active) return;
        Status = NotificationItemStatus.Cancelled;
        UpdatedAt = cancelledAt;
        Version++;
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (DeletedAt is not null) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        DeleteReason = reason;
        UpdatedAt = deletedAt;
        Version++;
    }
}
