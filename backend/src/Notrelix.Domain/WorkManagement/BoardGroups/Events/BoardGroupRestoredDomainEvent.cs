namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupRestoredDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
