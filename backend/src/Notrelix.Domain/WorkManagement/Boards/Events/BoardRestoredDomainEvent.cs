namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardRestoredDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
