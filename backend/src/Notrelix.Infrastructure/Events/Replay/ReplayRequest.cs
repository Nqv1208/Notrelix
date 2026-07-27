namespace Notrelix.Infrastructure.Events.Replay;

public sealed record ReplayRequest
{
    public required string EventName { get; init; }
    public int EventVersion { get; init; }
    public required string RequestedBy { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public string? Reason { get; init; }
    public string? CorrelationId { get; init; }
    public bool Authorized { get; init; }
    public string? SourceContextOverride { get; init; }
    public string? TraceParentOverride { get; init; }
}
