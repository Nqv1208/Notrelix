using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Identity;

[EventName("identity.user-registered", Version = 1)]
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid? ActorUserId,
    Guid? SourceEventId,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt
) : IntegrationEvent(
    messageName: "identity.user-registered",
    schemaVersion: 1,
    sourceEventId: SourceEventId,
    workspaceId: null,
    actorUserId: ActorUserId,
    correlationId: CorrelationId,
    causationId: CausationId,
    occurredAt: OccurredAt);
