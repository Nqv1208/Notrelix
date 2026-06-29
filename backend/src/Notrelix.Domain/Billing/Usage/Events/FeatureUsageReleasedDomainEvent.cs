namespace Notrelix.Domain.Billing.Usage.Events;

public record FeatureUsageReleasedDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Amount { get; }

    public FeatureUsageReleasedDomainEvent(
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
