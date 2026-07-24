namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-increased")]
public sealed record UsageMetricIncreasedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
