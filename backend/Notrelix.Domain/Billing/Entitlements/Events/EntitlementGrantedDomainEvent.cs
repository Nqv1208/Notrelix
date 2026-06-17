using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementGrantedDomainEvent : DomainEvent
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
        : base(occurredAt, workspaceId, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
        Limit = limit;
    }
}
