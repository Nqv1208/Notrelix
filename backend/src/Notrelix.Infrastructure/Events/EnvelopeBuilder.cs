using Notrelix.Infrastructure.Serialization;

namespace Notrelix.Infrastructure.Events;

public sealed record EnvelopeContext
{
    public required string EventName { get; init; }
    public int EventVersion { get; init; }
    public required string SourceContext { get; init; }
    public required string AggregateType { get; init; }
    public Guid AggregateId { get; init; }
    public required string SubjectType { get; init; }
    public Guid SubjectId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? TraceParent { get; init; }
    public string? TraceState { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? PartitionKey { get; init; }
    public string? TenantId { get; init; }
}

public sealed class EnvelopeBuilder
{
    private readonly IEventSerializer _serializer;
    private readonly IContractRegistry _contractRegistry;

    public EnvelopeBuilder(IEventSerializer serializer, IContractRegistry contractRegistry)
    {
        _serializer = serializer;
        _contractRegistry = contractRegistry;
    }

    public EventEnvelope Build(IIntegrationEvent integrationEvent, EnvelopeContext context)
    {
        var contract = _contractRegistry.GetByType(integrationEvent.GetType());
        var data = _serializer.Serialize(integrationEvent);

        return new EventEnvelope
        {
            Id = Guid.CreateVersion7(),
            EventName = context.EventName,
            EventVersion = context.EventVersion,
            SourceContext = context.SourceContext,
            AggregateType = context.AggregateType,
            AggregateId = context.AggregateId,
            SubjectType = context.SubjectType,
            SubjectId = context.SubjectId,
            WorkspaceId = context.WorkspaceId,
            ActorUserId = context.ActorUserId,
            CorrelationId = context.CorrelationId,
            CausationId = context.CausationId,
            TraceParent = context.TraceParent,
            TraceState = context.TraceState,
            OccurredAt = context.OccurredAt,
            Data = data,
            ContentType = EventEnvelope.DefaultContentType,
            Classification = contract.Classification,
            PartitionKey = context.PartitionKey,
            TenantId = context.TenantId,
        };
    }
}
