namespace Notrelix.Application.Events.Collaboration;

[EventName("comment.created", Version = 1)]
public sealed record CommentCreatedIntegrationEvent(
    Guid CommentId,
    Guid? WorkspaceId,
    string TargetType,
    Guid TargetId,
    Guid AuthorId,
    string Body,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "comment.created",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
