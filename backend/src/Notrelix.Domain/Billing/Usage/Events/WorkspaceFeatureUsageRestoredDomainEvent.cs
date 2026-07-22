namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
