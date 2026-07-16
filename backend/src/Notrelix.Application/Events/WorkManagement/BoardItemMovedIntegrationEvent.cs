namespace Notrelix.Application.Events.WorkManagement;

[EventName("board_item.moved", Version = 1)]
public sealed record BoardItemMovedIntegrationEvent(
    Guid EventId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    Guid? OldGroupId,
    Guid? NewGroupId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_item.moved",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
