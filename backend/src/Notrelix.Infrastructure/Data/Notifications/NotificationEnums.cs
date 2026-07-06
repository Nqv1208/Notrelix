namespace Notrelix.Infrastructure.Data.Notifications;

public enum NotificationItemStatus
{
    Active,
    Cancelled,
    Expired
}

public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error,
    Critical
}

public enum RecipientStatus
{
    Unread,
    Seen,
    Read,
    Archived,
    Dismissed
}

public enum NotificationChannel
{
    InApp,
    Email,
    Push,
    Slack,
    Webhook
}

public enum DeliveryMode
{
    Immediate,
    Digest,
    Muted
}
