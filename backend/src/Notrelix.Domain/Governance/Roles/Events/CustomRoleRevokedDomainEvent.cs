namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RevokedBy);
