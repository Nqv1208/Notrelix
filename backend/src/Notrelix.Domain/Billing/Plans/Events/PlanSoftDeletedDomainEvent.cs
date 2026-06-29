namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanSoftDeletedDomainEvent(
    Guid PlanId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
