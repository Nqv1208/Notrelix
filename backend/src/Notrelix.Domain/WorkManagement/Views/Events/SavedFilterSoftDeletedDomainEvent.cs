namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-soft-deleted")]
public sealed record SavedFilterSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
