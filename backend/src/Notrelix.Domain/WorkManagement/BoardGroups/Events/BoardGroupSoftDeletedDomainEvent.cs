namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
