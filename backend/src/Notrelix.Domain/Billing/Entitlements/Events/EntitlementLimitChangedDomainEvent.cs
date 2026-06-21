using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Entitlements.Events;

public record EntitlementLimitChangedDomainEvent : DomainEvent
{
    public Guid EntitlementId { get; }
    public string FeatureCode { get; }
    public decimal OldLimit { get; }
    public decimal NewLimit { get; }

    public EntitlementLimitChangedDomainEvent(
        Guid workspaceId,
        Guid entitlementId,
        string featureCode,
        decimal oldLimit,
        decimal newLimit,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        EntitlementId = entitlementId;
        FeatureCode = featureCode;
        OldLimit = oldLimit;
        NewLimit = newLimit;
    }
}
