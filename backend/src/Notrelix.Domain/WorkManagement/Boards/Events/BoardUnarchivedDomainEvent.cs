namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-unarchived")]
public sealed record BoardUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
