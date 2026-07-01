namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardBackgroundUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string OldBackground,
    string NewBackground,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
