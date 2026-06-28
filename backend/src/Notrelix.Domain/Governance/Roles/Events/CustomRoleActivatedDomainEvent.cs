namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleActivatedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActivatedBy);
