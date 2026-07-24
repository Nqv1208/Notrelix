namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-unarchived")]
public sealed record BoardViewUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
