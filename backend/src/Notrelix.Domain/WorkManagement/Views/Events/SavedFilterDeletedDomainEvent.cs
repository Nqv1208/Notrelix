namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-deleted")]
public sealed record SavedFilterDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
