namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-restored")]
public sealed record BoardItemRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
