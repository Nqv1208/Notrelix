namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupColorChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Color OldColor,
    Color NewColor,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
