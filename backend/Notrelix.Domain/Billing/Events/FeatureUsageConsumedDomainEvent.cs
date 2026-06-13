using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record FeatureUsageConsumedDomainEvent : DomainEvent
{
    public string FeatureCode { get; }
    public decimal Amount { get; }

    public FeatureUsageConsumedDomainEvent(
        Guid workspaceId,
        string featureCode,
        decimal amount,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        FeatureCode = featureCode;
        Amount = amount;
    }
}
