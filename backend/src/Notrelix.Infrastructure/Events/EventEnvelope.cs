namespace Notrelix.Infrastructure.Events;

public sealed record EventEnvelope
{
    public Guid Id { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int EventVersion { get; init; }
    public string SourceContext { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public Guid AggregateId { get; init; }
    public string SubjectType { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? TraceParent { get; init; }
    public string? TraceState { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public ReadOnlyMemory<byte> Data { get; init; }
    public string ContentType { get; init; } = DefaultContentType;
    public EventClassification Classification { get; init; }
    public string? PartitionKey { get; init; }
    public string? TenantId { get; init; }

    public const string DefaultContentType = "application/json";
}
