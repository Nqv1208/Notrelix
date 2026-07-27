namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-limit-added")]
public sealed record PlanLimitAddedDomainEvent(
    Guid PlanId,
    FeatureCode Feature,
    int Limit,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
