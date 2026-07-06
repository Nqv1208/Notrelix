namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
