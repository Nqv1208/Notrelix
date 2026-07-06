namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemToggledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    bool IsDone,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
