namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardBackgroundUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string OldBackground,
    string NewBackground,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
