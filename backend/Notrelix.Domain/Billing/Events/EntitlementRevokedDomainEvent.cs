using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record EntitlementRevokedDomainEvent : DomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementRevokedDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
    }
}
