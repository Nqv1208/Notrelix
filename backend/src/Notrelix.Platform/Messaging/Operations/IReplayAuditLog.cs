namespace Notrelix.Platform.Messaging.Operations;

public sealed record ReplayAuditEntry
{
    public long Id { get; init; }
    public string EventName { get; init; } = string.Empty;
    public Guid WorkspaceId { get; init; }
    public ReplayStrategyType StrategyType { get; init; }
    public long EventsRequested { get; init; }
    public long EventsPublished { get; init; }
    public long EventsFailed { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = "Running";
    public string? ErrorMessage { get; init; }
}

public interface IReplayAuditLog
{
    Task<long> StartAsync(ReplayRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(long auditId, ReplayResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReplayAuditEntry>> GetRecentAsync(int count = 20, CancellationToken cancellationToken = default);
}
