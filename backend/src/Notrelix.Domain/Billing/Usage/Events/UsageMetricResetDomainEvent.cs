namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-reset")]
public sealed record UsageMetricResetDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
