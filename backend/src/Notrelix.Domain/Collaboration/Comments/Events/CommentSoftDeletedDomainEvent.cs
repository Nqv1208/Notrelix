namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
