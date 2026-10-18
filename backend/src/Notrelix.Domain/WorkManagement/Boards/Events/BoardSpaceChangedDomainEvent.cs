namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-space-changed")]
public sealed record BoardSpaceChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid? OldSpaceId,
    Guid? NewSpaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
