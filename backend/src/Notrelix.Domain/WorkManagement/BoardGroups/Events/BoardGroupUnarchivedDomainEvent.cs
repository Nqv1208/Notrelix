namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid GroupId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UnarchivedBy);
