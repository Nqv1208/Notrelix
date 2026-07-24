namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-archived")]
public sealed record BoardArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
