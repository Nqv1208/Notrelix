namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-label-added")]
public sealed record BoardItemLabelAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
