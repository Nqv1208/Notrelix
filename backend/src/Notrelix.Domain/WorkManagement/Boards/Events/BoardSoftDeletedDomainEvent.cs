namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
