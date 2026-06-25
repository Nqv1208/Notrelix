namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupReorderedDomainEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid BoardId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
