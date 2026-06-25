namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemRemovedDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
