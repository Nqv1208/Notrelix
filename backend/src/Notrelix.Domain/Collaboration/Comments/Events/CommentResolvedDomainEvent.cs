namespace Notrelix.Domain.Collaboration.Comments.Events;

[EventName("collaboration.comment-resolved")]
public sealed record CommentResolvedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid ResolvedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
