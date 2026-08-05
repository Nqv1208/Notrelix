namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-completed", Version = 2)]
public sealed record BoardItemCompletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset CompletedAt,
    Guid CompletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
