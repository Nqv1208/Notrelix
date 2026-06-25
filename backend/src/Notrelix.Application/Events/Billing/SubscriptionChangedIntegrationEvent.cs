using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Billing;

[EventName("subscription.changed", Version = 1)]
public sealed record SubscriptionChangedIntegrationEvent(
    Guid SubscriptionId,
    Guid? WorkspaceId,
    Guid PreviousPlanId,
    Guid NewPlanId,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "subscription.changed",
    1,
    sourceEventId: null,
    WorkspaceId,
    actorUserId: null,
    CorrelationId,
    CausationId,
    OccurredAt
);
