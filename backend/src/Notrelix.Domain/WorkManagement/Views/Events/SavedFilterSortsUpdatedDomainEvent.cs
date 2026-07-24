namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-sorts-updated")]
public sealed record SavedFilterSortsUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
