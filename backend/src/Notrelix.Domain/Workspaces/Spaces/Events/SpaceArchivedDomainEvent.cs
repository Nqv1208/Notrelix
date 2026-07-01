namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
