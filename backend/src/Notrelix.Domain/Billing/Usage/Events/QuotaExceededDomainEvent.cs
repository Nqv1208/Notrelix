namespace Notrelix.Domain.Billing.Usage.Events;

public record QuotaExceededDomainEvent : WorkspaceScopedDomainEvent
{
    public string FeatureCode { get; }
    public decimal Limit { get; }

    public QuotaExceededDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string featureCode,
        decimal limit,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, null)
    {
        FeatureCode = featureCode;
        Limit = limit;
    }
}
