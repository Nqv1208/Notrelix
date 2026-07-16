namespace Notrelix.Application.Events.WorkManagement;

[EventName("board_item.renamed", Version = 1)]
public sealed record BoardItemRenamedIntegrationEvent(
    Guid EventId,
    Guid ItemId,
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
    messageName: "board_item.renamed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
