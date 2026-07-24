namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-soft-deleted")]
public sealed record UsageMetricSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
