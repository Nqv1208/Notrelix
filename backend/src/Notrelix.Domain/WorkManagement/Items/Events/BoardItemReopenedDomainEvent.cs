namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-reopened")]
public sealed record BoardItemReopenedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid ReopenedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
