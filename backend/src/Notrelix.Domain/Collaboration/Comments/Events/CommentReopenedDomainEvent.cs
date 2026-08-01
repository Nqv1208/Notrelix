namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-reopened")]
public sealed record CommentReopenedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid ReopenedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
