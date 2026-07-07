namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.created", Version = 1)]
public sealed record BoardCreatedIntegrationEvent(
    Guid EventId,
    Guid BoardId,
    Guid? WorkspaceId,
    string Name,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
