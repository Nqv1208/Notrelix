namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record UsageMetricSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    UsageMetricKey Key,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
