using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanSoftDeletedEvent(
    Guid PlanId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
