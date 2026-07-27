namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-soft-deleted")]
public sealed record ChecklistSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
