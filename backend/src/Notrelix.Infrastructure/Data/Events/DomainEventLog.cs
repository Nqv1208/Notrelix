using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Events;

public sealed class DomainEventLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string SourceContext { get; private set; } = null!;
    public string EventName { get; private set; } = null!;
    public int EventVersion { get; private set; } = 1;
    public string? AggregateType { get; private set; }
    public Guid? AggregateId { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public JsonDocument PayloadJson { get; private set; } = null!;
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset? RetentionUntil { get; private set; }

    private DomainEventLog() { }

    public DomainEventLog(
        Guid eventId,
        string sourceContext,
        string eventName,
        int eventVersion,
        string? aggregateType,
        Guid? aggregateId,
        string? subjectType,
        Guid? subjectId,
        Guid? workspaceId,
        Guid? actorUserId,
        string? correlationId,
        string? causationId,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        JsonDocument payloadJson,
        JsonDocument? metadataJson,
        DateTimeOffset? retentionUntil)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        SourceContext = sourceContext;
        EventName = eventName;
        EventVersion = eventVersion;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        PayloadJson = payloadJson;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        RetentionUntil = retentionUntil;
    }

    public static DomainEventLog FromDomainEvent(DomainEvent domainEvent, string eventName, DateTimeOffset now)
    {
        var payloadJson = JsonSerializer.SerializeToDocument(domainEvent, domainEvent.GetType(), JsonOptions);

        Guid? workspaceId = domainEvent is IWorkspaceScoped ws ? ws.WorkspaceId : null;
        Guid? actorUserId = null;

        return new DomainEventLog(
            eventId: domainEvent.EventId,
            sourceContext: domainEvent.SourceContext,
            eventName: eventName,
            eventVersion: domainEvent.EventVersion,
            aggregateType: domainEvent.AggregateType,
            aggregateId: domainEvent.AggregateId,
            subjectType: domainEvent.SubjectType,
            subjectId: domainEvent.SubjectId,
            workspaceId: workspaceId,
            actorUserId: actorUserId,
            correlationId: domainEvent.CorrelationId,
            causationId: domainEvent.CausationId,
            occurredAt: domainEvent.OccurredAt,
            recordedAt: now,
            payloadJson: payloadJson,
            metadataJson: null,
            retentionUntil: null);
    }
}
