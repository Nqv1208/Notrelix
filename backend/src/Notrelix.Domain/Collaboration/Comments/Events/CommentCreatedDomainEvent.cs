namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    ResourceRef Target,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, CreatedBy);
