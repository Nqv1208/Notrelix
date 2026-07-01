namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
