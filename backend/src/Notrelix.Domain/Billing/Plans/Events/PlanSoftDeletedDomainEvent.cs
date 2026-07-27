namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-soft-deleted")]
public sealed record PlanSoftDeletedDomainEvent(
    Guid PlanId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
