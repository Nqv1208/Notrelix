namespace Notrelix.Application.Events.Identity;

[EventName("identity.registration-completed", Version = 1)]
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
    occurredAt: OccurredAt)
{
    public Guid AccountIdValue => AccountId!.Value;
}