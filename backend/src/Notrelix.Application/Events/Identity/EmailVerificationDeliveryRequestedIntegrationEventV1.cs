namespace Notrelix.Application.Events.Identity;

[IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
[EventName("identity.email-verification-delivery-requested", Version = 1)]
[EventPiiField("Email",
    Purpose = "Address the verification email to the registering account owner.",
    ConsumerJustification = "SendEmailVerificationEmailConsumer performs delivery semantics; the address is the delivery target itself and cannot be derived from stable IDs by a downstream context.")]
[EventSensitiveField("ProtectedToken",
    Classification = "protected-single-use-verification-token",
    Justification = "Encrypted single-use token required to construct the verification URL inside the delivered email; it is not a raw credential, is bound to ExpiresAt, and must never be logged by outbox/retry/DLQ diagnostics.")]
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
