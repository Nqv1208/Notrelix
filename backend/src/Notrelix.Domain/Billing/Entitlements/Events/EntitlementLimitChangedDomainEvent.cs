namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementLimitChangedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }
    public decimal OldLimit { get; }
    public decimal NewLimit { get; }

    public EntitlementLimitChangedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid entitlementId,
        string featureCode,
        decimal oldLimit,
        decimal newLimit,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
        OldLimit = oldLimit;
        NewLimit = newLimit;
    }
}
