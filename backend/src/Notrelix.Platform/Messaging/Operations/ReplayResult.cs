namespace Notrelix.Platform.Messaging.Operations;

public sealed record ReplayResult
{
    public bool Success { get; init; }
    public long TotalRequested { get; init; }
    public long TotalPublished { get; init; }
    public long TotalFailed { get; init; }
    public TimeSpan Duration { get; init; }
    public long? CheckpointId { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ReplayResult Completed(long requested, long published, long failed, TimeSpan duration, long? checkpointId) =>
        new()
        {
            Success = true,
            TotalRequested = requested,
            TotalPublished = published,
            TotalFailed = failed,
            Duration = duration,
            CheckpointId = checkpointId,
        };

    public static ReplayResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error, Errors = [error] };

    public static ReplayResult Cancelled(long published, TimeSpan duration) =>
        new()
        {
            Success = false,
            TotalPublished = published,
            Duration = duration,
            ErrorMessage = "Replay cancelled",
        };
}
