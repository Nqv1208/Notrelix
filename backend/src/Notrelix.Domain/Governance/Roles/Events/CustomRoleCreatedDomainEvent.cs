namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleCreatedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, CreatedBy);
