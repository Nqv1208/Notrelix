namespace Notrelix.Domain.Chat.Messages.Events;

/// <summary>A person added a reaction to a message (Added true) or removed their reaction (Added false), UC-CHT-G06, G07.</summary>
[EventName("chat.reaction-changed")]
public sealed record MessageReactionChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    Guid ActorId,
    string Kind,
    bool Added,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
