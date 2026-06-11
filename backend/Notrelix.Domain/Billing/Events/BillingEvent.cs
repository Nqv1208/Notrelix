using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public class BillingEvent : AggregateRoot
{
    public string ProviderEventId { get; private set; } = null!;
    public BillingEventType Type { get; private set; }
    public BillingEventStatus Status { get; private set; }
    public JsonValue RawData { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public string? Error { get; private set; }

    private BillingEvent() : base() { }

    public static BillingEvent Record(string providerEventId, BillingEventType type, JsonValue rawData)
    {
        Guard.NotNullOrWhiteSpace(providerEventId);
        Guard.NotNull(rawData);

        return new BillingEvent
        {
            ProviderEventId = providerEventId,
            Type = type,
            Status = BillingEventStatus.Received,
            RawData = rawData,
            ReceivedAt = DateTimeOffset.UtcNow
        };
    }
}
