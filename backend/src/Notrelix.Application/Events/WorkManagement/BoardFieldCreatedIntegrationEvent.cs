namespace Notrelix.Application.Events.WorkManagement;

[EventName("board_field.created", Version = 1)]
public sealed record BoardFieldCreatedIntegrationEvent(
    Guid EventId,
    Guid FieldId,
    Guid BoardId,
    Guid? WorkspaceId,
    string FieldName,
    string FieldType,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_field.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
