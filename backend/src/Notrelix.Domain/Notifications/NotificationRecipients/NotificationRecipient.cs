using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Notifications.NotificationRecipients;

public class NotificationRecipient : Entity, IWorkspaceScoped
{
    public Guid NotificationId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? RecipientName { get; private set; }
    public JsonValue DeliveryPolicyJson { get; private set; } = JsonValue.EmptyObject();
    public RecipientStatus Status { get; private set; }
    public DateTimeOffset? SeenAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DismissedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationRecipient() : base() { }

    public static NotificationRecipient Create(
        Guid notificationId,
        Guid workspaceId,
        Guid recipientUserId,
        DateTimeOffset createdAt,
        string? recipientEmail = null,
        string? recipientName = null,
        JsonValue? deliveryPolicyJson = null)
    {
        Guard.NotEmpty(notificationId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(recipientUserId);

        return new NotificationRecipient
        {
            NotificationId = notificationId,
            WorkspaceId = workspaceId,
            RecipientUserId = recipientUserId,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            DeliveryPolicyJson = deliveryPolicyJson ?? JsonValue.EmptyObject(),
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
