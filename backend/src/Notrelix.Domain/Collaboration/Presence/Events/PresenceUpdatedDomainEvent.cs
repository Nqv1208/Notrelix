namespace Notrelix.Domain.Collaboration.Presence.Events;

[EventName("collaboration.presence-updated")]
public sealed record PresenceUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
