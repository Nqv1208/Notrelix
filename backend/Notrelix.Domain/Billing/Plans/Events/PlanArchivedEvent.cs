using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Plans.Events;

public sealed record PlanArchivedEvent(
    Guid PlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
