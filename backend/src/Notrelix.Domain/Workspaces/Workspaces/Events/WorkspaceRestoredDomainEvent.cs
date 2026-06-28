namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceRestoredDomainEvent(
    Guid WorkspaceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
