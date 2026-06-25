namespace Notrelix.Domain.Billing.Usage.Events;

public record QuotaExceededDomainEvent : DomainEvent
{
    public string FeatureCode { get; }
    public decimal Limit { get; }

    public QuotaExceededDomainEvent(
        Guid workspaceId,
        string featureCode,
        decimal limit,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, null)
    {
        FeatureCode = featureCode;
        Limit = limit;
    }
}
