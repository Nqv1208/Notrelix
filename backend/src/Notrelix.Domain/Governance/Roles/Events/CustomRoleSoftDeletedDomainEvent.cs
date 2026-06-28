namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
