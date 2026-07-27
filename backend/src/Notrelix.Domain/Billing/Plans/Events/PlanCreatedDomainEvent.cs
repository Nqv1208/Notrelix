namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-created")]
public sealed record PlanCreatedDomainEvent(
    Guid PlanId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
