namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemAddedDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    string Title,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
