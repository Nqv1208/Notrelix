namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

[EventName("work-management.board-group-color-changed")]
public sealed record BoardGroupColorChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Color OldColor,
    Color NewColor,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
