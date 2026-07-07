namespace Notrelix.Application.Events.Billing;

[EventName("subscription.changed", Version = 1)]
public sealed record SubscriptionChangedIntegrationEvent(
    Guid EventId,
    Guid SubscriptionId,
    Guid? WorkspaceId,
    Guid PreviousPlanId,
    Guid NewPlanId,
    Guid CorrelationId,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "subscription.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: null,
    causationId: CausationId,
    occurredAt: OccurredAt
);
