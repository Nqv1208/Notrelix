using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricRestoredEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
