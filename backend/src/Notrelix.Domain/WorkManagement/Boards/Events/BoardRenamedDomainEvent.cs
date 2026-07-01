namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
