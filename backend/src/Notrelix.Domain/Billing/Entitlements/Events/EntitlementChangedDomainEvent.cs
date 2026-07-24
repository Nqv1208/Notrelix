using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Entitlements.Events;

[EventName("billing.entitlement-changed")]
public sealed record EntitlementChangedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    string FeatureCode,
    int NewLimit,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
