namespace Notrelix.Domain.Billing.Plans.Events;

[EventName("billing.plan-description-updated")]
public sealed record PlanDescriptionUpdatedDomainEvent(
    Guid PlanId,
    string? Description,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
