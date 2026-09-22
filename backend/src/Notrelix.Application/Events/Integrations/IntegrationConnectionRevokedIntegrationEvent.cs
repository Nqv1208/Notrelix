namespace Notrelix.Application.Events.Integrations;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("integrations.integration-connection-revoked", Version = 1)]
public sealed record IntegrationConnectionRevokedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid ConnectionId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "integrations.integration-connection-revoked",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
