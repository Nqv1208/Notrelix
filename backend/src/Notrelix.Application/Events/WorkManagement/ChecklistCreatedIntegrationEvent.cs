namespace Notrelix.Application.Events.WorkManagement;

[EventName("checklist.created", Version = 1)]
public sealed record ChecklistCreatedIntegrationEvent(
    Guid EventId,
    Guid ChecklistId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    string ChecklistTitle,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "checklist.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
