namespace Notrelix.Application.Events.Workspaces;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("workspace.archived", Version = 1)]
public sealed record WorkspaceArchivedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.archived",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: null,
    occurredAt: OccurredAt
);

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("workspace.unarchived", Version = 1)]
public sealed record WorkspaceUnarchivedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.unarchived",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: null,
    occurredAt: OccurredAt
);
