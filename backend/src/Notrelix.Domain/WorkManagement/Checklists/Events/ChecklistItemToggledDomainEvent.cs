namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemToggledDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    bool IsDone,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
