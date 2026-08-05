using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Entitlements.Events;

[EventName("billing.entitlement-revoked")]
public sealed record EntitlementRevokedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementRevokedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid entitlementId,
        string featureCode,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
