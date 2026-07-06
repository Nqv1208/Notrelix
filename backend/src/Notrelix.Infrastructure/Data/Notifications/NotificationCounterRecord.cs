namespace Notrelix.Infrastructure.Data.Notifications;

public sealed class NotificationCounterRecord
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string CounterType { get; private set; } = "Notification";
    public int CounterValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public long Version { get; private set; } = 1;

    private NotificationCounterRecord() { }

    public static NotificationCounterRecord Create(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        string counterType,
        DateTimeOffset createdAt)
    {
        return new NotificationCounterRecord
        {
            AccountId = accountId,
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
