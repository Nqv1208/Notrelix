namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanRestoredDomainEvent(
    Guid PlanId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
