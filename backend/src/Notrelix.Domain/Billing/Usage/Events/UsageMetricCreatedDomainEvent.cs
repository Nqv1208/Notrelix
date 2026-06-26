namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricCreatedDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
