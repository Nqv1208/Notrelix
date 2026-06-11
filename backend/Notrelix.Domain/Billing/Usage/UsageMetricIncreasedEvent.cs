using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Usage;

public sealed record UsageMetricIncreasedEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
