namespace Notrelix.Application.Events.Billing;

[EventName("subscription.canceled", Version = 1)]
public sealed record SubscriptionCanceledIntegrationEvent(
    Guid EventId,
    Guid SubscriptionId,
    Guid? WorkspaceId,
    DateTimeOffset EffectiveAt,
    Guid CorrelationId,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "subscription.canceled",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: null,
    causationId: CausationId,
    occurredAt: OccurredAt
);
