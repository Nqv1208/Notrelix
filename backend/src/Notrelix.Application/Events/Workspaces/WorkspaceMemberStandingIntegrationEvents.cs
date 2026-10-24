namespace Notrelix.Application.Events.Workspaces;

/// <summary>
/// Workspaces-owned fact: a member was suspended. Published so contexts whose
/// state depends on that member's standing (for example Automation rules that run
/// as the member) can react without reading Workspaces storage.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("workspace.member.suspended", Version = 1)]
public sealed record WorkspaceMemberSuspendedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid UserId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.member.suspended",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);

/// <summary>
/// Workspaces-owned fact: a member's workspace role changed. Carries the old and
/// new role names and whether Workspaces ranks the change as a demotion, so
/// consumers never reimplement Workspaces role ordering.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("workspace.member.role-changed", Version = 1)]
public sealed record WorkspaceMemberRoleChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid UserId,
    string OldRole,
    string NewRole,
    bool IsDemotion,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.member.role-changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
