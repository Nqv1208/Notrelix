namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricResetDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
