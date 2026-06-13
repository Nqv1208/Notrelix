using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

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
