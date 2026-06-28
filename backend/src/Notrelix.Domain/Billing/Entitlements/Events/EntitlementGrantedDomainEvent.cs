namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementGrantedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }
    public decimal Limit { get; }

    public EntitlementGrantedDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        decimal limit,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
        Limit = limit;
    }
}
