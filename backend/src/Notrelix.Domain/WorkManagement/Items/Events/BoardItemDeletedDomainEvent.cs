namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-deleted")]
public sealed record BoardItemDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
