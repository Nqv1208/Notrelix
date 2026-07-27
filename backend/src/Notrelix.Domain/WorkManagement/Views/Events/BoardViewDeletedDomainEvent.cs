namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-deleted")]
public sealed record BoardViewDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
