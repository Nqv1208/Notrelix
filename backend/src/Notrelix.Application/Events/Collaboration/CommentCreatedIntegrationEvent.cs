using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

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
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "comment.created",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
