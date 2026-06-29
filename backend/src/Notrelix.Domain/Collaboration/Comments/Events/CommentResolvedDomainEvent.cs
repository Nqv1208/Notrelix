namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentResolvedDomainEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid ResolvedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ResolvedBy);
