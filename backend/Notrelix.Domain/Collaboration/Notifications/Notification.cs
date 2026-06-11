using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Notifications;

public class Notification : AggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public ResourceRef? Target { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private Notification() : base() { }

    public static Notification Create(
        Guid userId, 
        NotificationType type, 
        string title, 
        string content, 
        ResourceRef? target = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNullOrWhiteSpace(content);

        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Content = content.Trim(),
            Target = target,
            Status = NotificationStatus.Unread,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
    }

    public void Archive()
    {
        if (Status == NotificationStatus.Archived) return;
        Status = NotificationStatus.Archived;
    }
}
