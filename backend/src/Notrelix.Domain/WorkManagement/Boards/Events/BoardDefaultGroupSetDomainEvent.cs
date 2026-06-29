namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardDefaultGroupSetDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
