namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementRestoredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
