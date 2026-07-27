namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-revoked")]
public sealed record CustomRoleRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
