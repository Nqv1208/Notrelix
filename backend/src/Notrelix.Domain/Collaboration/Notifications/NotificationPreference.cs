using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Notifications;

public enum NotificationChannel
{
    InApp,
    Email,
    Push
}

public class NotificationPreference : Entity
{
    public Guid UserId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public bool Enabled { get; private set; }

    private NotificationPreference() : base() { }

    public static NotificationPreference Create(Guid userId, NotificationChannel channel, Guid? workspaceId = null, bool enabled = true)
    {
        Guard.NotEmpty(userId);

        return new NotificationPreference
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Channel = channel,
            Enabled = enabled
        };
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }
}
