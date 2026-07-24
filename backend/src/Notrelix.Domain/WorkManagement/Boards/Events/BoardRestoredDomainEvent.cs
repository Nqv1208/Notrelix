namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-restored")]
public sealed record BoardRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
