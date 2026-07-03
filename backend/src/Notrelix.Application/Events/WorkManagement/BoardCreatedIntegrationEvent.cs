using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.created", Version = 1)]
public sealed record BoardCreatedIntegrationEvent(
    Guid BoardId,
    Guid? WorkspaceId,
    string Name,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "board.created",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
