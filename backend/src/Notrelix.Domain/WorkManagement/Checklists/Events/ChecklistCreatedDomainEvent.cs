namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistCreatedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
