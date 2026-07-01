namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterVisibilityUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    SavedFilterVisibility Visibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
