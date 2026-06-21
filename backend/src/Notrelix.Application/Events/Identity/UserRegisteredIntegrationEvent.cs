using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Identity;

[EventName("user.registered", Version = 1)]
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "user.registered",
    1,
    sourceEventId: null,
    workspaceId: null,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
