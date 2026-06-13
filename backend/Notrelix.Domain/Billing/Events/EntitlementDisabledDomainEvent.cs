using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record EntitlementDisabledDomainEvent : DomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }

    public EntitlementDisabledDomainEvent(
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
