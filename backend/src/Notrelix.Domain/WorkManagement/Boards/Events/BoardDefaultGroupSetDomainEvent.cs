namespace Notrelix.Domain.WorkManagement.Boards.Events;

[EventName("work-management.board-default-group-set")]
public sealed record BoardDefaultGroupSetDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
