using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Billing;

[EventName("subscription.changed", Version = 1)]
public sealed record SubscriptionChangedIntegrationEvent(
    Guid SubscriptionId,
    Guid? WorkspaceId,
    Guid PreviousPlanId,
    Guid NewPlanId,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "subscription.changed",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    actorUserId: null,
    CorrelationId,
    CausationId,
    OccurredAt
);
