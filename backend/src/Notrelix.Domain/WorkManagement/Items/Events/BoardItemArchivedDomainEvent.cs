namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-archived")]
public sealed record BoardItemArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
