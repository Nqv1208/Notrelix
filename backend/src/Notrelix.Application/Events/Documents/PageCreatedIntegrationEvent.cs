namespace Notrelix.Application.Events.Documents;

[EventName("page.created", Version = 1)]
public sealed record PageCreatedIntegrationEvent(
    Guid EventId,
    Guid PageId,
    Guid? WorkspaceId,
    string Title,
    Guid? ParentId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "page.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
