namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-soft-deleted")]
public sealed record CustomRoleSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
