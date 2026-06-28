namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentRestoredDomainEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
