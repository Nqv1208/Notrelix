namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-deleted")]
public sealed record CommentDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
