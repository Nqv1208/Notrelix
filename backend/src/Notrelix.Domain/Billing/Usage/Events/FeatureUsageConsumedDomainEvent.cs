namespace Notrelix.Domain.Billing.Usage.Events;

public record FeatureUsageConsumedDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Amount { get; }

    public FeatureUsageConsumedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string featureCode,
        decimal amount,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FeatureCode = featureCode;
        Amount = amount;
    }
}
