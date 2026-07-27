namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-archived")]
public sealed record PlanArchivedDomainEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
