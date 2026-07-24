namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-soft-deleted")]
public sealed record CommentSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
