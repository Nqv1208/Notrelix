using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricResetEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
