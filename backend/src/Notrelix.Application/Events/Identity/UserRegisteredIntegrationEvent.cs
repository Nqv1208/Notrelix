namespace Notrelix.Application.Events.Identity;

[EventName("identity.user-registered", Version = 2)]
public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Email,
    string DisplayName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.user-registered",
    schemaVersion: 2,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
