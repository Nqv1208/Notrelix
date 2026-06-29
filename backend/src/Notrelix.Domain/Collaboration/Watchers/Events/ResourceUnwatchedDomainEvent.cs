namespace Notrelix.Domain.Collaboration.Watchers.Events;

public sealed record ResourceUnwatchedDomainEvent(
    Guid WorkspaceId,
    Guid WatcherId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
