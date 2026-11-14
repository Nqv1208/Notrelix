namespace Notrelix.Application.Events.Identity;

/// <summary>
/// Someone asked to register an address that already has an account. Registration answers the same neutral
/// "check your email" for a new and for an existing address, so the person behind the existing address is
/// told by mail instead; this fact carries only the address the notice goes to.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
[EventName("identity.registration-attempted-on-existing-account", Version = 1)]
[EventPiiField("Email",
    Purpose = "Address the notice goes to: the owner of the account the registration attempt named.",
    ConsumerJustification = "SendRegistrationNoticeEmailConsumer queues the notice; the address is the delivery target itself and cannot be derived from stable IDs by a downstream context.")]
public sealed record RegistrationAttemptedOnExistingAccountIntegrationEventV1(
    Guid EventId,
    string Email,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.registration-attempted-on-existing-account",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
