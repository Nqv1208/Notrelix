namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-restored")]
public sealed record PlanRestoredDomainEvent(
    Guid PlanId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
