namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanArchivedDomainEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
