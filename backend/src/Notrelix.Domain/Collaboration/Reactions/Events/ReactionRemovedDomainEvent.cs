namespace Notrelix.Domain.Collaboration.Reactions.Events;

public sealed record ReactionRemovedDomainEvent(
    Guid WorkspaceId,
    Guid ReactionId,
    ResourceRef Target,
    Guid UserId,
    Emoji Emoji,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
