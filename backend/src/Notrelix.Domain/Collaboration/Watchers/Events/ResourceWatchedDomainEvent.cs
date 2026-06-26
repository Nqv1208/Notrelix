namespace Notrelix.Domain.Collaboration.Watchers.Events;

public sealed record ResourceWatchedDomainEvent(
    Guid WorkspaceId,
    Guid WatcherId,
    ResourceRef Target,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
