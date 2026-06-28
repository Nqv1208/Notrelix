namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupArchivedDomainEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
