namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-renamed")]
public sealed record BoardRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
