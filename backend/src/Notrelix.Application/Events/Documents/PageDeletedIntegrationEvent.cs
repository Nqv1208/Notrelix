namespace Notrelix.Application.Events.Documents;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("page.deleted", Version = 1)]
public sealed record PageDeletedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid PageId,
    Guid? WorkspaceId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "page.deleted",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
