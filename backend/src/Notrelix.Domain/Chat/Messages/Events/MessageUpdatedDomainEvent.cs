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
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
