namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricCreatedDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
