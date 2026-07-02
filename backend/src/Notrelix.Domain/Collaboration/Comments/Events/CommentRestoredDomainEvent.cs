namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid CommentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
