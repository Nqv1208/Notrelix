namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-metric-created")]
public sealed record UsageMetricCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
