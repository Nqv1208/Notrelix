using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record EntitlementExpiredDomainEvent : DomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementExpiredDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, null)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
