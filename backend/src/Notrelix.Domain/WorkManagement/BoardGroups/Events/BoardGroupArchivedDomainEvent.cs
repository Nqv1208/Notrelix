namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

[EventName("work-management.board-group-archived")]
public sealed record BoardGroupArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid GroupId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
