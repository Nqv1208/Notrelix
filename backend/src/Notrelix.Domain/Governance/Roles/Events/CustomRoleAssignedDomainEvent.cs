namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleAssignedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, AssignedBy);
