namespace Notrelix.Application.Events.WorkManagement;

[EventName("board_view.created", Version = 1)]
public sealed record BoardViewCreatedIntegrationEvent(
    Guid EventId,
    Guid ViewId,
    Guid BoardId,
    Guid? WorkspaceId,
    string ViewName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_view.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
