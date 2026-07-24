namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.quota-exceeded")]
public sealed record QuotaExceededDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Limit { get; }

    public QuotaExceededDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string featureCode,
        decimal limit,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FeatureCode = featureCode;
        Limit = limit;
    }
}
