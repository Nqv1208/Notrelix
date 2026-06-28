namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterRenamedDomainEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    string Name,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
