namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-deleted")]
public sealed record ChecklistDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
