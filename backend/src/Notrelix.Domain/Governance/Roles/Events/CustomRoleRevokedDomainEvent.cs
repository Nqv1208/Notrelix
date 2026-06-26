namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleRevokedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RevokedBy);
