namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
