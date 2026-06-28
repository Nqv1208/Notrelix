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
        EnsureNotDeleted();
        if (Status != BillingEventStatus.Received) return;

        Status = BillingEventStatus.Processed;
        SetAuditOnUpdate(updatedBy, processedAt);
        IncrementVersion();
    }

    public void MarkFailed(string error, Guid updatedBy, DateTimeOffset failedAt)
    {
        EnsureNotDeleted();
        if (Status == BillingEventStatus.Failed) return;

        Status = BillingEventStatus.Failed;
        Error = error;
        SetAuditOnUpdate(updatedBy, failedAt);
        IncrementVersion();
    }

    public void MarkIgnored(Guid updatedBy, DateTimeOffset ignoredAt)
    {
        EnsureNotDeleted();
        if (Status != BillingEventStatus.Received) return;

        Status = BillingEventStatus.Ignored;
        SetAuditOnUpdate(updatedBy, ignoredAt);
        IncrementVersion();
    }
}
