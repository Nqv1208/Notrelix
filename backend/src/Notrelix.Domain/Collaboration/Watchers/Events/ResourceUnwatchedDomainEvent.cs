namespace Notrelix.Domain.Collaboration.Watchers.Events;

[EventName("collaboration.resource-unwatched")]
public sealed record ResourceUnwatchedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid WatcherId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
