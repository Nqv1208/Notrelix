namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-label-removed")]
public sealed record BoardItemLabelRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    string LabelName,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
