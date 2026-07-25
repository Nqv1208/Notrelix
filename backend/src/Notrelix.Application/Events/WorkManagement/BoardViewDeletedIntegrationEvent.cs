namespace Notrelix.Application.Events.WorkManagement;

[EventName("board_view.deleted", Version = 1)]
public sealed record BoardViewDeletedIntegrationEvent(
    Guid EventId,
    Guid ViewId,
    Guid BoardId,
    Guid? WorkspaceId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_view.deleted",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
