namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("checklist.created", Version = 1)]
public sealed record ChecklistCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
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
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
