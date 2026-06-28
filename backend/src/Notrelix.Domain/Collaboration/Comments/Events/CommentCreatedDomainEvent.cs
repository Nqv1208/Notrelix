namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentCreatedDomainEvent(
    Guid WorkspaceId,
    Guid CommentId,
    ResourceRef Target,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
