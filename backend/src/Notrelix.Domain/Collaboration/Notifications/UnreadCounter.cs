namespace Notrelix.Domain.Collaboration.Notifications;

public enum UnreadCounterType
{
    Notification,
    Mention,
    AssignedItem
}

public class UnreadCounter : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public UnreadCounterType CounterType { get; private set; }
    public int CounterValue { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UnreadCounter() : base() { }

    public static UnreadCounter Create(
        Guid workspaceId,
        Guid userId,
        UnreadCounterType counterType,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        return new UnreadCounter
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            CounterType = counterType,
            CounterValue = 0,
            UpdatedAt = createdAt
        };
    }

    public void Increment(DateTimeOffset updatedAt)
    {
        CounterValue++;
        UpdatedAt = updatedAt;
    }

    public void Decrement(DateTimeOffset updatedAt)
    {
        if (CounterValue > 0)
        {
            CounterValue--;
        }
        UpdatedAt = updatedAt;
    }

    public void Reset(DateTimeOffset updatedAt)
    {
        CounterValue = 0;
        UpdatedAt = updatedAt;
    }

    public void SetValue(int val, DateTimeOffset updatedAt)
    {
        if (val < 0)
            throw new ArgumentOutOfRangeException(nameof(val), "Counter value cannot be negative.");
        CounterValue = val;
        UpdatedAt = updatedAt;
    }
}
