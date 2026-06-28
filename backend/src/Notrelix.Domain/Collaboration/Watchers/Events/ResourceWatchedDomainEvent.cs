namespace Notrelix.Domain.Collaboration.Watchers.Events;

public sealed record ResourceWatchedDomainEvent(
    Guid WorkspaceId,
    Guid WatcherId,
    ResourceRef Target,
    Guid UserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
