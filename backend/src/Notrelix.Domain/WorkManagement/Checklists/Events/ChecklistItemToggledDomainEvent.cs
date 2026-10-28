namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-item-toggled")]
public sealed record ChecklistItemToggledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    bool IsDone,
    Guid BoardItemId,
    string ItemTitle,
    string ChecklistTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
