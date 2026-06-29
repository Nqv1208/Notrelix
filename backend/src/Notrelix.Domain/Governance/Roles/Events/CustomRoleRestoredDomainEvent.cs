namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleRestoredDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
