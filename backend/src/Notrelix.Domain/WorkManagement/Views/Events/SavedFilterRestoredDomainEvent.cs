namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-restored")]
public sealed record SavedFilterRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
