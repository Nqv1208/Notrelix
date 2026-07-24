namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-archived")]
public sealed record CustomRoleArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
