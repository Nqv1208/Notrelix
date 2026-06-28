namespace Notrelix.Domain.Billing.Usage.Events;

public record FeatureUsageConsumedDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Amount { get; }

    public FeatureUsageConsumedDomainEvent(
        Guid workspaceId,
        string featureCode,
        decimal amount,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        FeatureCode = featureCode;
        Amount = amount;
    }
}
