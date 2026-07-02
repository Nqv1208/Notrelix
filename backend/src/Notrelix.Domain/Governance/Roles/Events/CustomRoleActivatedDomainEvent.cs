namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ActivatedBy);
