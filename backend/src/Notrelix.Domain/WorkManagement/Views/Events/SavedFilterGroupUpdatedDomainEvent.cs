namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterGroupUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
