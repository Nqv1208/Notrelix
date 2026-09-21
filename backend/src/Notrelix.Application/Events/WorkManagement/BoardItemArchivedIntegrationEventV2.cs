namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("board_item.archived", Version = 2)]
public sealed record BoardItemArchivedIntegrationEventV2(
    Guid EventId,
    Guid? AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    long Revision,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_item.archived",
    schemaVersion: 2,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);