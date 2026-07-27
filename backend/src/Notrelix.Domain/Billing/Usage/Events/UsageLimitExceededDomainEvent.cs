namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.usage-limit-exceeded")]
public sealed record UsageLimitExceededDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
