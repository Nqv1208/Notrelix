namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementDisabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementDisabledDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
