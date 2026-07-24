namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-updated")]
public sealed record CustomRoleUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
