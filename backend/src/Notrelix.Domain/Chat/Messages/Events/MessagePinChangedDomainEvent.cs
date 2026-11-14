namespace Notrelix.Domain.Chat.Messages.Events;

/// <summary>A message was pinned (Pinned true) or its pin ended by an unpin or a deletion (Pinned false), UC-CHT-F06, F07.</summary>
[EventName("chat.pin-changed")]
public sealed record MessagePinChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    Guid ActorId,
    bool Pinned,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
