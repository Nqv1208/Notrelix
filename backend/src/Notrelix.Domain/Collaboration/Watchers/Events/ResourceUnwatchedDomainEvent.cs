namespace Notrelix.Domain.Collaboration.Watchers.Events;

public sealed record ResourceUnwatchedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid WatcherId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
