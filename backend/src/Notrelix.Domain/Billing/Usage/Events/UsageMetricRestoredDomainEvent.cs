namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricRestoredDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
