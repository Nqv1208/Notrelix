namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanLimitAddedDomainEvent(
    Guid PlanId,
    FeatureCode Feature,
    int Limit,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
