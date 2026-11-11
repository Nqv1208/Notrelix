namespace Notrelix.Domain.Chat.Messages.Events;

[EventName("chat.message-created")]
public sealed record MessageCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MessageId,
    Guid SpaceId,
    long Sequence,
    Guid AuthorId,
    MessageKind Kind,
    string? Content,
    string? ClientMessageId,
    long Version,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
