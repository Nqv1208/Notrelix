namespace Notrelix.Application.Events.Collaboration;

[EventName("comment.created", Version = 1)]
public sealed record CommentCreatedIntegrationEvent(
    Guid EventId,
    Guid CommentId,
    Guid? WorkspaceId,
    string TargetType,
    Guid TargetId,
    Guid AuthorId,
    string Body,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "comment.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
