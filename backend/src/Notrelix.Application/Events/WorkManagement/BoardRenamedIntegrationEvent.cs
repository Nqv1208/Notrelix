namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.renamed", Version = 1)]
public sealed record BoardRenamedIntegrationEvent(
    Guid EventId,
    Guid BoardId,
    Guid? WorkspaceId,
    string OldName,
    string NewName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board.renamed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
