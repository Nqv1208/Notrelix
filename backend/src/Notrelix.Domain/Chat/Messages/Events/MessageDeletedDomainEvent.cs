namespace Notrelix.Domain.Chat.Messages.Events;

[EventName("chat.message-deleted")]
public sealed record MessageDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MessageId,
    Guid SpaceId,
    long Sequence,
    Guid AuthorId,
    Guid DeletedBy,
    long Version,
    DateTimeOffset OccurredAt,
    Guid? ThreadRootId = null
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
