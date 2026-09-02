namespace Notrelix.Application.Events.Identity;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Account)]
[EventName("identity.registration-completed", Version = 1)]
[EventPiiField("Email",
    Purpose = "Deliver the welcome communication and identify the account contact.",
    ConsumerJustification = "SendWelcomeEmailConsumer performs delivery semantics; downstream contexts cannot resolve the address from stable IDs without a private Identity read.")]
[EventPiiField("DisplayName",
    Purpose = "Personalize the welcome communication with the chosen display identity.",
    ConsumerJustification = "SendWelcomeEmailConsumer renders the greeting using the registration-time display name; no approved read contract exists for consumers to look it up afterwards.")]
public sealed record IdentityRegistrationCompletedIntegrationEventV1(
    Guid EventId,
    Guid UserId,
    Guid? AccountId,
    string Email,
    string DisplayName,
    string AccountName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.registration-completed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: AccountId,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt,
    requireAccountId: true)
{
    public Guid AccountIdValue => AccountId!.Value;
}