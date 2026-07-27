namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-visibility-updated")]
public sealed record SavedFilterVisibilityUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    SavedFilterVisibility Visibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
