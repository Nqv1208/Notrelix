namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-deprecated")]
public sealed record PlanDeprecatedDomainEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
