using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanDeprecatedEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
