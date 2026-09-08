namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-reply-created")]
public sealed record CommentReplyCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid ParentCommentId,
    ResourceRef Target,
    Guid CreatedBy,
    string Content,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
