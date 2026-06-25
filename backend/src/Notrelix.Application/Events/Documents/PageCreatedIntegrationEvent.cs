using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Documents;

[EventName("page.created", Version = 1)]
public sealed record PageCreatedIntegrationEvent(
    Guid PageId,
    Guid? WorkspaceId,
    string Title,
    Guid? ParentId,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "page.created",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
