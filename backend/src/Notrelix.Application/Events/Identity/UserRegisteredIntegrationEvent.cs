using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Identity;

[EventName("identity.user-registered", Version = 2)]
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    Guid AccountId,
    string Email,
    string DisplayName,
    Guid? ActorUserId,
    Guid? SourceEventId,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt
) : IntegrationEvent(
    messageName: "identity.user-registered",
    schemaVersion: 2,
    sourceEventId: SourceEventId,
    workspaceId: null,
    actorUserId: ActorUserId,
    correlationId: CorrelationId,
    causationId: CausationId,
    occurredAt: OccurredAt);
