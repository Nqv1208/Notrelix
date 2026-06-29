namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemToggledDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    bool IsDone,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
