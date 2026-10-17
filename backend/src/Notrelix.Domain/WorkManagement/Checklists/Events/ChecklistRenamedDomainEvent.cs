namespace Notrelix.Domain.WorkManagement.Checklists.Events;

[EventName("work-management.checklist-renamed")]
public sealed record ChecklistRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
