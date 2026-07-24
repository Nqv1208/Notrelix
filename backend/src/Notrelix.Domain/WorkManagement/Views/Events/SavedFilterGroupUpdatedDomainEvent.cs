namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-group-updated")]
public sealed record SavedFilterGroupUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
