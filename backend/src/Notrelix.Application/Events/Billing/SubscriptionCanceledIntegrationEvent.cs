using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Billing;

[EventName("subscription.canceled", Version = 1)]
public sealed record SubscriptionCanceledIntegrationEvent(
    Guid SubscriptionId,
    Guid? WorkspaceId,
    DateTimeOffset EffectiveAt,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "subscription.canceled",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    actorUserId: null,
    CorrelationId,
    CausationId,
    OccurredAt
);
