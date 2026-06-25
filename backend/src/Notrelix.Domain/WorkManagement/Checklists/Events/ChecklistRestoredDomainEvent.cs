namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistRestoredDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
