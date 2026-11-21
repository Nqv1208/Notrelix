namespace Notrelix.Application.Events.Documents;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("page.renamed", Version = 1)]
public sealed record PageRenamedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid PageId,
    Guid? WorkspaceId,
    string OldTitle,
    string NewTitle,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "page.renamed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
