namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-created")]
public sealed record CustomRoleCreatedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
