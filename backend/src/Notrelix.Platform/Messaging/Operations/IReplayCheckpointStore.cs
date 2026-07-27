namespace Notrelix.Platform.Messaging.Operations;

public sealed record ReplayCheckpoint
{
    public long Id { get; init; }
    public string EventName { get; init; } = string.Empty;
    public Guid WorkspaceId { get; init; }
    public long EventPosition { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public interface IReplayCheckpointStore
{
    Task<ReplayCheckpoint?> GetLatestAsync(string eventName, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<ReplayCheckpoint?> GetByIdAsync(long checkpointId, CancellationToken cancellationToken = default);
    Task<ReplayCheckpoint> SaveAsync(string eventName, Guid workspaceId, long eventPosition, CancellationToken cancellationToken = default);
    Task DeleteAsync(long checkpointId, CancellationToken cancellationToken = default);
}
