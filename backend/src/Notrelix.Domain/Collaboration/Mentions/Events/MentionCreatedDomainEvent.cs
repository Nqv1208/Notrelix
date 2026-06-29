namespace Notrelix.Domain.Collaboration.Mentions.Events;

public sealed record MentionCreatedDomainEvent(
    Guid WorkspaceId,
    Guid MentionId,
    ResourceRef Source,
    Guid MentionedId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
