namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageLimitExceededDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
