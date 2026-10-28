namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-item-removed")]
public sealed record ChecklistItemRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    Guid BoardItemId,
    string ItemTitle,
    string ChecklistTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
