namespace Notrelix.Domain.Collaboration.Mentions.Events;

[EventName("collaboration.mention-created")]
public sealed record MentionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MentionId,
    ResourceRef Source,
    Guid MentionedId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
