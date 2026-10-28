namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-item-added")]
public sealed record ChecklistItemAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    string Title,
    Guid BoardItemId,
    string ChecklistTitle,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
