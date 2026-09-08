namespace Notrelix.Application.Events.Workspaces;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("space.created", Version = 1)]
public sealed record SpaceCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    string Name,
    string Visibility,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "space.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: null,
    occurredAt: OccurredAt
);
