namespace Notrelix.Domain.Collaboration.Watchers.Events;

[EventName("collaboration.resource-watched")]
public sealed record ResourceWatchedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid WatcherId,
    ResourceRef Target,
    Guid UserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
