namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
