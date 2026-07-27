namespace Notrelix.Platform.Messaging.Runtime;

public sealed record PublishContext
{
    public required Guid CorrelationId { get; init; }
    public Guid CausationId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid? AccountId { get; init; }
    public string? SourceContext { get; init; }
    public string? AggregateType { get; init; }
    public Guid? AggregateId { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public string? TraceParent { get; init; }
    public string? TraceState { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed record EventPublication
{
    public required object Event { get; init; }
    public required PublishContext Context { get; init; }
}
