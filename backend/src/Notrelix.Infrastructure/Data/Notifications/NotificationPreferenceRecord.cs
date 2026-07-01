using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

public sealed class NotificationPreferenceRecord
{
    public Guid Id { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; }
    public DeliveryMode DeliveryMode { get; private set; }
    public int? DigestIntervalMinutes { get; private set; }
    public JsonDocument QuietHoursJson { get; private set; } = JsonDocument.Parse("{}");
    public string? Timezone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationPreferenceRecord() { }

    public static NotificationPreferenceRecord Create(
        Guid userId,
        string notificationType,
        NotificationChannel channel,
        DateTimeOffset createdAt,
        Guid? workspaceId = null,
        bool isEnabled = true,
        DeliveryMode deliveryMode = DeliveryMode.Immediate,
        int? digestIntervalMinutes = null,
        JsonDocument? quietHoursJson = null,
        string? timezone = null)
    {
        return new NotificationPreferenceRecord
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            UserId = userId,
            NotificationType = notificationType.Trim(),
            Channel = channel,
            IsEnabled = isEnabled,
            DeliveryMode = deliveryMode,
            DigestIntervalMinutes = digestIntervalMinutes,
            QuietHoursJson = quietHoursJson ?? JsonDocument.Parse("{}"),
            Timezone = timezone,
            CreatedAt = createdAt
        };
    }

    public void SetEnabled(bool enabled, DateTimeOffset updatedAt)
    {
        if (IsEnabled == enabled) return;
        IsEnabled = enabled;
        UpdatedAt = updatedAt;
    }

    public void SetDeliveryMode(DeliveryMode mode, DateTimeOffset updatedAt)
    {
        DeliveryMode = mode;
        UpdatedAt = updatedAt;
    }
}
