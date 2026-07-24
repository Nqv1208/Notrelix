namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-restored")]
public sealed record UsageMetricRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
