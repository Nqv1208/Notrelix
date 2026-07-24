namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

[EventName("work-management.board-group-soft-deleted")]
public sealed record BoardGroupSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
