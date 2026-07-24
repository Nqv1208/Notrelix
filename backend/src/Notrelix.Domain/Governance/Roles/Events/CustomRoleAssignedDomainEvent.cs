namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-assigned")]
public sealed record CustomRoleAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
