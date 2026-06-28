namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceRenamedDomainEvent(
    Guid WorkspaceId,
    Guid SpaceId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
