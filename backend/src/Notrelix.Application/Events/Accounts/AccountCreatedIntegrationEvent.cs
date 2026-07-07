namespace Notrelix.Application.Events.Accounts;

[EventName("account.created", Version = 1)]
public sealed record AccountCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid OwnerUserId,
    string Name,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "account.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: AccountId,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
