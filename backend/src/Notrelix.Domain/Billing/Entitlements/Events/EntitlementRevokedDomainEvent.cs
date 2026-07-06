namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementRevokedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementRevokedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid entitlementId,
        string featureCode,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
