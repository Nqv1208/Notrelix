namespace Notrelix.Platform.Messaging.Operations;

public sealed record ReplayRequest
{
    public string EventName { get; init; } = string.Empty;
    public Guid WorkspaceId { get; init; }
    public ReplayStrategyType StrategyType { get; init; } = ReplayStrategyType.Latest;
    public long? CheckpointId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int MaxEventsPerSecond { get; init; } = 100;
    public string? Source { get; init; }
}

public enum ReplayStrategyType
{
    Latest,
    Checkpoint,
    Snapshot,
    TimeWindow,
}
