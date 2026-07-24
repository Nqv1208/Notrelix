namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-updated")]
public sealed record CommentUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
