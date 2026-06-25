namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
