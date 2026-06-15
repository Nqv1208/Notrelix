using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanDescriptionUpdatedEvent(
    Guid PlanId,
    string? Description,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
