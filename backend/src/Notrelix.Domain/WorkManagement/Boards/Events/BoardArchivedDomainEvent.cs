namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardArchivedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
