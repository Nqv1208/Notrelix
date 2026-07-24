namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-soft-deleted")]
public sealed record BoardItemSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
