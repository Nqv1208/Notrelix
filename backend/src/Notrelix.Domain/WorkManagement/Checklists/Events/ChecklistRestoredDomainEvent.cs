namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-restored")]
public sealed record ChecklistRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
