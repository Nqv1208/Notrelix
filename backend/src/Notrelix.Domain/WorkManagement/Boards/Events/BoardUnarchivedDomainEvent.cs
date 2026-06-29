namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardUnarchivedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UnarchivedBy);
