namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-deleted")]
public sealed record ChecklistDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
