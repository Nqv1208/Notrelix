namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid GroupId,
    Guid BoardId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
