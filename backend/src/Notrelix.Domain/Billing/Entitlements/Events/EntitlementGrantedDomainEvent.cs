using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Entitlements.Events;

[EventName("billing.entitlement-granted")]
public sealed record EntitlementGrantedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }
    public decimal Limit { get; }
    public bool IsUnlimited { get; }

    public EntitlementGrantedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid entitlementId,
        string featureCode,
        decimal limit,
        DateTimeOffset occurredAt,
        bool isUnlimited = false)
        : base(accountId, workspaceId, occurredAt)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
        Limit = limit;
        IsUnlimited = isUnlimited;
    }
}
