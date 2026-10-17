namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-moved")]
public sealed record ChecklistMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    FractionalIndex NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
