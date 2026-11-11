namespace Notrelix.Domain.Chat.Messages.Events;

[EventName("chat.message-updated")]
public sealed record MessageUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MessageId,
    Guid SpaceId,
    long Sequence,
    Guid AuthorId,
    string Content,
    long Version,
    DateTimeOffset OccurredAt,
    Guid? ThreadRootId = null
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
