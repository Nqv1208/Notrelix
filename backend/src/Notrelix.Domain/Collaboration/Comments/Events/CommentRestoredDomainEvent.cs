namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-restored")]
public sealed record CommentRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
