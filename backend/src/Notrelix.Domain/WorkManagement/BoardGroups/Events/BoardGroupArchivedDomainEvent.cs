namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupArchivedDomainEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
