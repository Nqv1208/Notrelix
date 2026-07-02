namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricDecreasedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
