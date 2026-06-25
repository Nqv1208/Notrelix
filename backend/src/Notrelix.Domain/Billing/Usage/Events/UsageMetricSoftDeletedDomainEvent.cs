namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricSoftDeletedDomainEvent(
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
