using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Billing;

[EventName("subscription.canceled", Version = 1)]
public sealed record SubscriptionCanceledIntegrationEvent(
    Guid SubscriptionId,
    Guid? WorkspaceId,
    DateTimeOffset EffectiveAt,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "subscription.canceled",
    1,
    sourceEventId: null,
    WorkspaceId,
    actorUserId: null,
    CorrelationId,
    CausationId,
    OccurredAt
);
