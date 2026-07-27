namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-activated")]
public sealed record CustomRoleActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
