namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementExpiredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementExpiredDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, null)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
