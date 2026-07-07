namespace Notrelix.Application.Events.Identity;

[EventName("user.deactivated", Version = 1)]
public sealed record UserDeactivatedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "user.deactivated",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
