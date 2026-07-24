using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Entitlements.Events;

[EventName("billing.entitlement-restored")]
public sealed record EntitlementRestoredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
