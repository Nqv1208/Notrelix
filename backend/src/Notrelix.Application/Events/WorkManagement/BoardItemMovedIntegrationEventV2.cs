namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("board_item.moved", Version = 2)]
public sealed record BoardItemMovedIntegrationEventV2(
    Guid EventId,
    Guid? AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    Guid? OldGroupId,
    Guid? NewGroupId,
    long Revision,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_item.moved",
    schemaVersion: 2,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);