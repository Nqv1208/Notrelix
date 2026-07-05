namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementChangedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    string FeatureCode,
    int NewLimit,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
