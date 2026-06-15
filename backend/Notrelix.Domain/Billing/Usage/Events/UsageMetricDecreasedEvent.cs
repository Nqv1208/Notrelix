using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricDecreasedEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
