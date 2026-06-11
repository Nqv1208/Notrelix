using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Plans;

public sealed record PlanCreatedEvent(
    Guid PlanId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
