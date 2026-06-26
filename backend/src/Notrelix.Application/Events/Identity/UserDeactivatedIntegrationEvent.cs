using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Identity;

[EventName("user.deactivated", Version = 1)]
public sealed record UserDeactivatedIntegrationEvent(
    Guid UserId,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "user.deactivated",
    1,
    sourceEventId: null,
    workspaceId: null,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
