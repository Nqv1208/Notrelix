namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.feature-usage-released")]
public sealed record FeatureUsageReleasedDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Amount { get; }

    public FeatureUsageReleasedDomainEvent(
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
