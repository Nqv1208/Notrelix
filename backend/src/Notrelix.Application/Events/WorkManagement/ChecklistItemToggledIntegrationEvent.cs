namespace Notrelix.Application.Events.WorkManagement;

[EventName("checklist_item.toggled", Version = 1)]
public sealed record ChecklistItemToggledIntegrationEvent(
    Guid EventId,
    Guid ChecklistId,
    Guid ChecklistItemId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    bool IsCompleted,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "checklist_item.toggled",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
