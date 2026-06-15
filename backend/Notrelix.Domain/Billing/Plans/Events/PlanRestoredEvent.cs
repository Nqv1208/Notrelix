using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanRestoredEvent(
    Guid PlanId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
