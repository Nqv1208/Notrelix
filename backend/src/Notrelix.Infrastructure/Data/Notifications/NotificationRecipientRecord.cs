using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

public sealed class NotificationRecipientRecord
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid NotificationId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? RecipientName { get; private set; }
    public JsonDocument DeliveryPolicyJson { get; private set; } = JsonDocument.Parse("{}");
    public RecipientStatus Status { get; private set; }
    public DateTimeOffset? SeenAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DismissedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationRecipientRecord() { }

    public static NotificationRecipientRecord Create(
        Guid accountId,
        Guid notificationId,
        Guid workspaceId,
        Guid recipientUserId,
        DateTimeOffset createdAt,
        string? recipientEmail = null,
        string? recipientName = null,
        JsonDocument? deliveryPolicyJson = null)
    {
        return new NotificationRecipientRecord
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            NotificationId = notificationId,
            WorkspaceId = workspaceId,
            RecipientUserId = recipientUserId,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            DeliveryPolicyJson = deliveryPolicyJson ?? JsonDocument.Parse("{}"),
            Status = RecipientStatus.Unread,
            CreatedAt = createdAt
        };
    }

    public void MarkAsSeen(DateTimeOffset seenAt)
    {
        if (Status != RecipientStatus.Unread) return;
        Status = RecipientStatus.Seen;
        SeenAt = seenAt;
        UpdatedAt = seenAt;
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (Status == RecipientStatus.Read || Status == RecipientStatus.Archived) return;
        Status = RecipientStatus.Read;
        ReadAt = readAt;
        SeenAt ??= readAt;
        UpdatedAt = readAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == RecipientStatus.Archived) return;
        Status = RecipientStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    public void Dismiss(DateTimeOffset dismissedAt)
    {
        if (Status == RecipientStatus.Dismissed) return;
        Status = RecipientStatus.Dismissed;
        DismissedAt = dismissedAt;
        UpdatedAt = dismissedAt;
    }
}
