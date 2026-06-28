namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanDeprecatedDomainEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
