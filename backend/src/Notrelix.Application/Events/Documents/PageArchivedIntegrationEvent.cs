using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Documents;

[EventName("page.archived", Version = 1)]
public sealed record PageArchivedIntegrationEvent(
    Guid PageId,
    Guid? WorkspaceId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "page.archived",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
