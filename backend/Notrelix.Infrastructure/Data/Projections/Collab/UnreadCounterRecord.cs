namespace Notrelix.Infrastructure.Data.Projections.Collab;

public sealed class UnreadCounterRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string CounterType { get; private set; } = null!;
    public int CounterValue { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UnreadCounterRecord() { }

    public static UnreadCounterRecord Create(
        Guid id,
        Guid workspaceId,
        Guid userId,
        string counterType,
        int counterValue,
        DateTimeOffset updatedAt)
    {
        return new UnreadCounterRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            UserId = userId,
            CounterType = counterType,
            CounterValue = counterValue,
            UpdatedAt = updatedAt,
        };
    }
}
