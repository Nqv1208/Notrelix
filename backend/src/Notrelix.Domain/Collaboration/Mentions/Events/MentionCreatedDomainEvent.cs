namespace Notrelix.Domain.Collaboration.Mentions.Events;

[EventName("collaboration.mention-created")]
public sealed record MentionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MentionId,
    ResourceRef Source,
    Guid MentionedId,
    Guid MentionedByUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
