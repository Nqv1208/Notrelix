namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterCreatedDomainEvent(
    Guid FilterId,
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt,
    Guid? ViewId = null
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
