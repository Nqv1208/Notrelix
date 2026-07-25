namespace Notrelix.Application.Events.Identity;

[EventName("identity.email-verification-delivery-requested", Version = 1)]
public sealed record EmailVerificationDeliveryRequestedIntegrationEventV1(
    Guid EventId,
    Guid VerificationTokenId,
    Guid UserId,
    string Email,
    string ProtectedToken,
    int HashVersion,
    DateTimeOffset ExpiresAt,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.email-verification-delivery-requested",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
