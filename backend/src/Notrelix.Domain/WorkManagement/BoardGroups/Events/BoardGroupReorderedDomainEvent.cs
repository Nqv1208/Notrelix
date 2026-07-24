namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

[EventName("work-management.board-group-reordered")]
public sealed record BoardGroupReorderedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid GroupId,
    Guid BoardId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
