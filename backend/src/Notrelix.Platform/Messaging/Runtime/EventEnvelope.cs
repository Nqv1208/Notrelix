using Notrelix.Application.Common.Events;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed record EventEnvelope
{
    public Guid Id { get; init; }
    public required string EventName { get; init; }
    public int EventVersion { get; init; }

    /// <summary>
    /// Producer-assigned ordering sequence within the aggregate/partition. Ordered
    /// consumers validate this value; a null sequence is auto-assigned the next
    /// expected value in arrival order so ordered delivery never silently drops.
    /// </summary>
    public long? Sequence { get; init; }
    public string? SourceContext { get; init; }
    public string? AggregateType { get; init; }
    public Guid? AggregateId { get; init; }
    public string? SubjectType { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid CausationId { get; init; }
    public string? TraceParent { get; init; }
    public string? TraceState { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required ReadOnlyMemory<byte> Data { get; init; }
    public required string ContentType { get; init; }
    public EventClassification Classification { get; init; }
    public ReadOnlyMemory<byte>? CanonicalPayload { get; init; }
    public Guid? AccountId { get; init; }
}
