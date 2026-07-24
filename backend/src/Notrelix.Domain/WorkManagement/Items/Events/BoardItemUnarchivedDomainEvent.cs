namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-unarchived")]
public sealed record BoardItemUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
