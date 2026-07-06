namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RoleId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ArchivedBy);
