using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Identity;

[EventName("user.deactivated", Version = 1)]
public sealed record UserDeactivatedIntegrationEvent(
    Guid UserId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "user.deactivated",
    1,
    sourceEventId: null,
    accountId: null,
    workspaceId: null,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
