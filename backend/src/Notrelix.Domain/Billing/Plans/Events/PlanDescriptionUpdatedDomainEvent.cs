namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanDescriptionUpdatedDomainEvent(
    Guid PlanId,
    string? Description,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
