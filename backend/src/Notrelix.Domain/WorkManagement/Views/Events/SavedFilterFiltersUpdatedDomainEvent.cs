namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-filters-updated")]
public sealed record SavedFilterFiltersUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
