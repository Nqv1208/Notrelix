namespace Notrelix.Application.Events.Governance;

[EventName("governance.role.assigned", Version = 1)]
public sealed record CustomRoleAssignedIntegrationEvent(
    Guid EventId,
    Guid RoleId,
    Guid? WorkspaceId,
    string RoleName,
    Guid UserId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "governance.role.assigned",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
