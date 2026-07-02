namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
