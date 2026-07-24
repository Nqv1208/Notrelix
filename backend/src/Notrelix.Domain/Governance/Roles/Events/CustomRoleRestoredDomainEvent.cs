namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-restored")]
public sealed record CustomRoleRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
