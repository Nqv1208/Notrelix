namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementSoftDeletedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
