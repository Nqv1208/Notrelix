namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterRestoredDomainEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
