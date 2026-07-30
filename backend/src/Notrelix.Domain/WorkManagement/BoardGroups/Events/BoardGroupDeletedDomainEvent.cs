namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

[EventName("work-management.board-group-deleted")]
public sealed record BoardGroupDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
