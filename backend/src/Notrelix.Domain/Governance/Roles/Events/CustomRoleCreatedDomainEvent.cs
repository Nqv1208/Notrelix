namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleCreatedDomainEvent(
    Guid RoleId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
