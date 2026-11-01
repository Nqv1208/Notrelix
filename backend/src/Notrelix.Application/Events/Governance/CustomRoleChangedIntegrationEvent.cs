namespace Notrelix.Application.Events.Governance;

/// <summary>
/// A custom role changed: <c>Created</c>, <c>Updated</c>, <c>Archived</c> (the definition), or <c>Assigned</c>,
/// <c>Removed</c> (one person in one workspace, named by <see cref="UserId"/>).
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Account)]
[EventName("governance.role.changed", Version = 1)]
public sealed record CustomRoleChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid RoleId,
    string RoleName,
    string ChangeKind,
    Guid? UserId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "governance.role.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
