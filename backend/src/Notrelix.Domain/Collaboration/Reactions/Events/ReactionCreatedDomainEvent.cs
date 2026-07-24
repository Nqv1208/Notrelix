namespace Notrelix.Domain.Collaboration.Reactions.Events;

[EventName("collaboration.reaction-created")]
public sealed record ReactionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ReactionId,
    ResourceRef Target,
    Guid UserId,
    Emoji Emoji,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
