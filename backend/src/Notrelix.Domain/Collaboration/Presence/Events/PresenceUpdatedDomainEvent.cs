namespace Notrelix.Domain.Collaboration.Presence.Events;

public sealed record PresenceUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
