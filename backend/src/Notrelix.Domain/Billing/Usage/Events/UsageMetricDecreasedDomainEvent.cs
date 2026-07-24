namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-decreased")]
public sealed record UsageMetricDecreasedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
