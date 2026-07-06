namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.item.created", Version = 1)]
public sealed record BoardItemCreatedIntegrationEvent(
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    string Title,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "board.item.created",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
