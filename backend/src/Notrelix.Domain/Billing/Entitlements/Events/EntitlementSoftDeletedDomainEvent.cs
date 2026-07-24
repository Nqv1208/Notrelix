using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Entitlements.Events;

[EventName("billing.entitlement-soft-deleted")]
public sealed record EntitlementSoftDeletedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
