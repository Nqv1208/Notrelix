namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageLimitExceededDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
