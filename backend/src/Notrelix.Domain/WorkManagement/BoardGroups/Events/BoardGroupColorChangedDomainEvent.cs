namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupColorChangedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Color OldColor,
    Color NewColor,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
