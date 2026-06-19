using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Collaboration;

[EventName("mention.created", Version = 1)]
public sealed record MentionCreatedIntegrationEvent(
    Guid MentionId,
    Guid? WorkspaceId,
    string TargetType,
    Guid TargetId,
    Guid MentionedUserId,
    Guid MentionedByUserId,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "mention.created",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
