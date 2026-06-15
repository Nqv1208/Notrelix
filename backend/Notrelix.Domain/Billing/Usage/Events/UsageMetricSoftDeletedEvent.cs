using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricSoftDeletedEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
