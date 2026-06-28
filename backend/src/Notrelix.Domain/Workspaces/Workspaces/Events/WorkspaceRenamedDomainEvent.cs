namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceRenamedDomainEvent(
    Guid WorkspaceId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
