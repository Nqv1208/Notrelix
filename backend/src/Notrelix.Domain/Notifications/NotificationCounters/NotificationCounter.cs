using Notrelix.Domain.Common;

namespace Notrelix.Domain.Notifications.NotificationCounters;

public class NotificationCounter : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string CounterType { get; private set; } = "Notification";
    public int CounterValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public long Version { get; private set; } = 1;

    private NotificationCounter() : base() { }

    public static NotificationCounter Create(
        Guid workspaceId,
        Guid userId,
        string counterType,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(counterType);

        return new NotificationCounter
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            CounterType = counterType,
            CounterValue = 0,
            UpdatedAt = createdAt,
            CreatedAt = createdAt
        };
    }

    public void Increment(DateTimeOffset now)
    {
        CounterValue++;
        UpdatedAt = now;
        Version++;
    }

    public void Reset(DateTimeOffset now)
    {
        CounterValue = 0;
        UpdatedAt = now;
        Version++;
    }
}
