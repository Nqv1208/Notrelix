namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterFiltersUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
