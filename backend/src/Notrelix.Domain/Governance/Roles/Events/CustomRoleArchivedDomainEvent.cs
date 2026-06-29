namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleArchivedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
