namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricIncreasedDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    int Amount,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
