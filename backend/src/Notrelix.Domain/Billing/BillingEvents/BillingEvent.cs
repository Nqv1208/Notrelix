namespace Notrelix.Domain.Billing.BillingEvents;

public class BillingEvent : AggregateRoot
{
    public string ProviderEventId { get; private set; } = null!;
    public BillingEventType Type { get; private set; }
    public BillingEventStatus Status { get; private set; }
    public JsonValue RawData { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public string? Error { get; private set; }

    private BillingEvent() : base() { }

    public static BillingEvent Record(string providerEventId, BillingEventType type, JsonValue rawData, DateTimeOffset receivedAt)
    {
        Guard.NotNullOrWhiteSpace(providerEventId);
        Guard.NotNull(rawData);

        return new BillingEvent
        {
            ProviderEventId = providerEventId,
            Type = type,
            Status = BillingEventStatus.Received,
            RawData = rawData,
            ReceivedAt = receivedAt
        };
    }

    public void MarkProcessed(Guid updatedBy, DateTimeOffset processedAt)
    {
        if (Status != BillingEventStatus.Received) return;

        var pending = PrepareAuditUpdate(updatedBy, processedAt);
        Status = BillingEventStatus.Processed;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void MarkFailed(string error, Guid updatedBy, DateTimeOffset failedAt)
    {
        if (Status == BillingEventStatus.Failed) return;

        var pending = PrepareAuditUpdate(updatedBy, failedAt);
        Status = BillingEventStatus.Failed;
        Error = error;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void MarkIgnored(Guid updatedBy, DateTimeOffset ignoredAt)
    {
        if (Status != BillingEventStatus.Received) return;

        var pending = PrepareAuditUpdate(updatedBy, ignoredAt);
        Status = BillingEventStatus.Ignored;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }
}
