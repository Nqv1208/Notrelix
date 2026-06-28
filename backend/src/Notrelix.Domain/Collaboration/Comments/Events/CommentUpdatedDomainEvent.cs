namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
