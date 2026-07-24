namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-background-updated")]
public sealed record BoardBackgroundUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string OldBackground,
    string NewBackground,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
