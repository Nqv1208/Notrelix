namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.saved-filter-renamed")]
public sealed record SavedFilterRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    string Name,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
