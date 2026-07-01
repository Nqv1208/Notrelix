namespace Notrelix.Domain.Collaboration.Reactions.Events;

public sealed record ReactionRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ReactionId,
    ResourceRef Target,
    Guid UserId,
    Emoji Emoji,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
