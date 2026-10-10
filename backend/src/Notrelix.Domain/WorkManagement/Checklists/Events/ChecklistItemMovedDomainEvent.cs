namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-item-moved")]
public sealed record ChecklistItemMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    FractionalIndex NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
