namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanCreatedDomainEvent(
    Guid PlanId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
