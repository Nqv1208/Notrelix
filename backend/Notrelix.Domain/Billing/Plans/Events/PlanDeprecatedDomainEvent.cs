using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanDeprecatedDomainEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
