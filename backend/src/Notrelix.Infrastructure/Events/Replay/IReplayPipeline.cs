namespace Notrelix.Infrastructure.Events.Replay;

public interface IReplayPipeline
{
    Task<ReplayResult> ExecuteAsync(ReplayRequest request, CancellationToken cancellationToken = default);
}

public sealed record ReplayResult
{
    public bool Success { get; init; }
    public int EventsReplayed { get; init; }
    public int EventsFailed { get; init; }
    public List<string> Errors { get; init; } = [];
    public string? ReplayCorrelationId { get; init; }
}
