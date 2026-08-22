namespace Notrelix.Application.Events.Identity;

[EventName("identity.user-registered", Version = 2)]
[EventPiiField("Email",
    Purpose = "Deliver the welcome communication to the newly registered account owner.",
    ConsumerJustification = "SendWelcomeEmailConsumer performs delivery semantics and cannot resolve the address from stable IDs without a private Identity read, which is forbidden for downstream contexts.")]
[EventPiiField("DisplayName",
    Purpose = "Personalize the welcome communication with the chosen display identity.",
    ConsumerJustification = "SendWelcomeEmailConsumer renders the greeting using the display name captured at registration; no approved read contract exists for post-registration display-name lookup by consumers.")]
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
