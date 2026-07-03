using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Governance;

[EventName("governance.role.assigned", Version = 1)]
public sealed record CustomRoleAssignedIntegrationEvent(
    Guid RoleId,
    Guid? WorkspaceId,
    string RoleName,
    Guid UserId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "governance.role.assigned",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
