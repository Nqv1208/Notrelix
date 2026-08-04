namespace Notrelix.Domain.Integrations.Sync;

public sealed class SyncCursorValue : ValueObject
{
    public string Value { get; } = null!;

    private SyncCursorValue() { }
    private SyncCursorValue(string value)
    {
        Value = value;
    }

    public static SyncCursorValue Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new SyncCursorValue(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

public class IntegrationSyncCursor : Entity
{
    public Guid ConnectionId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public SyncCursorValue Cursor { get; private set; } = null!;
    public DateTimeOffset LastSyncedAt { get; private set; }

    private IntegrationSyncCursor() : base() { }

    public static IntegrationSyncCursor Create(Guid connectionId, string resourceType, SyncCursorValue cursor, DateTimeOffset lastSyncedAt)
    {
        Guard.NotEmpty(connectionId);
        Guard.NotNullOrWhiteSpace(resourceType);
        Guard.NotNull(cursor);

        return new IntegrationSyncCursor
        {
            ConnectionId = connectionId,
            ResourceKind = resourceType,
            Cursor = cursor,
            LastSyncedAt = lastSyncedAt
        };
    }

    public void UpdateCursor(SyncCursorValue newCursor, DateTimeOffset lastSyncedAt)
    {
        Guard.NotNull(newCursor);
        Cursor = newCursor;
        LastSyncedAt = lastSyncedAt;
    }
}
