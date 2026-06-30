using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Notifications.NotificationPreferences;

public class NotificationPreference : Entity
{
    public Guid? WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; }
    public DeliveryMode DeliveryMode { get; private set; }
    public int? DigestIntervalMinutes { get; private set; }
    public JsonValue QuietHoursJson { get; private set; } = JsonValue.EmptyObject();
    public string? Timezone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationPreference() : base() { }

    public static NotificationPreference Create(
        Guid userId,
        string notificationType,
        NotificationChannel channel,
        DateTimeOffset createdAt,
        Guid? workspaceId = null,
        bool isEnabled = true,
        DeliveryMode deliveryMode = DeliveryMode.Immediate,
        int? digestIntervalMinutes = null,
        JsonValue? quietHoursJson = null,
        string? timezone = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(notificationType);

        if (digestIntervalMinutes.HasValue && digestIntervalMinutes.Value <= 0)
            throw new BusinessRuleException("DigestIntervalMinutes must be positive.");

        return new NotificationPreference
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            NotificationType = notificationType.Trim(),
            Channel = channel,
            IsEnabled = isEnabled,
            DeliveryMode = deliveryMode,
            DigestIntervalMinutes = digestIntervalMinutes,
            QuietHoursJson = quietHoursJson ?? JsonValue.EmptyObject(),
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
