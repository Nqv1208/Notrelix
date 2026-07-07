namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.item.created", Version = 1)]
public sealed record BoardItemCreatedIntegrationEvent(
    Guid EventId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    string Title,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board.item.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
