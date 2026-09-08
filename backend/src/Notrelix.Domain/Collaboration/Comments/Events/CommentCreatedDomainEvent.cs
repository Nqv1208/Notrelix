namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-created")]
public sealed record CommentCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    ResourceRef Target,
    Guid CreatedBy,
    string Content,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
