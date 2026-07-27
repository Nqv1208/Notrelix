namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-soft-deleted")]
public sealed record BoardSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
