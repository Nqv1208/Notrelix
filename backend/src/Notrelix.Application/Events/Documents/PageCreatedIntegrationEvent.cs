namespace Notrelix.Application.Events.Documents;

[EventName("page.created", Version = 1)]
public sealed record PageCreatedIntegrationEvent(
    Guid PageId,
    Guid? WorkspaceId,
    string Title,
    Guid? ParentId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "page.created",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
