using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Notifications;

public enum NotificationDeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
    Cancelled
}

public class NotificationDelivery : Entity, IWorkspaceScoped
{
    public Guid NotificationId { get; private set; }
    public Guid WorkspaceId { get; private set; } // Implements IWorkspaceScoped for tenant isolation
    public Guid RecipientUserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private NotificationDelivery() : base() { }

    public static NotificationDelivery Create(
        Guid notificationId,
        Guid workspaceId,
        Guid recipientUserId,
        NotificationChannel channel,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(notificationId);
        // Note: WorkspaceId might be empty for system-wide notifications, but for SaaS, all are scoped.
        // We enforce workspace isolation here.
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(recipientUserId);

        return new NotificationDelivery
        {
            NotificationId = notificationId,
            WorkspaceId = workspaceId,
            RecipientUserId = recipientUserId,
            Channel = channel,
            Status = NotificationDeliveryStatus.Pending,
            CreatedAt = createdAt
        };
    }

    public void MarkSent(string? providerMessageId, DateTimeOffset sentAt)
    {
        if (Status != NotificationDeliveryStatus.Pending)
            throw new BusinessRuleException("Only pending deliveries can be marked as sent.");

        Status = NotificationDeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        SentAt = sentAt;
    }

    public void MarkFailed(string errorMessage, DateTimeOffset failedAt)
    {
        if (Status != NotificationDeliveryStatus.Pending)
            throw new BusinessRuleException("Only pending deliveries can be marked as failed.");
        Guard.NotNullOrWhiteSpace(errorMessage);

        Status = NotificationDeliveryStatus.Failed;
        ErrorMessage = errorMessage;
        SentAt = failedAt;
    }

    public void Skip(DateTimeOffset skippedAt)
    {
        if (Status != NotificationDeliveryStatus.Pending)
            throw new BusinessRuleException("Only pending deliveries can be skipped.");

        Status = NotificationDeliveryStatus.Skipped;
        SentAt = skippedAt;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status != NotificationDeliveryStatus.Pending)
            throw new BusinessRuleException("Only pending deliveries can be cancelled.");

        Status = NotificationDeliveryStatus.Cancelled;
        SentAt = cancelledAt;
    }
}
