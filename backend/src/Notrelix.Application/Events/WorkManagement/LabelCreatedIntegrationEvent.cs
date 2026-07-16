namespace Notrelix.Application.Events.WorkManagement;

[EventName("label.created", Version = 1)]
public sealed record LabelCreatedIntegrationEvent(
    Guid EventId,
    Guid LabelId,
    Guid BoardId,
    Guid? WorkspaceId,
    string LabelName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "label.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
