namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceRestoredDomainEvent(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
