namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-created")]
public sealed record ChecklistCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
