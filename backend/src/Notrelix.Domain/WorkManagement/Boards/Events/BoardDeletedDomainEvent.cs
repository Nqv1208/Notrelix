namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-deleted")]
public sealed record BoardDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
