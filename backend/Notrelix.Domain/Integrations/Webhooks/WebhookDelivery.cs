using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Webhooks;

public class WebhookDelivery : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid WebhookSubscriptionId { get; private set; }
    public WebhookEventType EventType { get; private set; }
    public JsonValue Payload { get; private set; } = null!;
    public WebhookDeliveryStatus Status { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private WebhookDelivery() : base() { }

    public static WebhookDelivery Create(Guid workspaceId, Guid subscriptionId, WebhookEventType eventType, JsonValue payload)
    {
        return new WebhookDelivery
        {
            WorkspaceId = workspaceId,
            WebhookSubscriptionId = subscriptionId,
            EventType = eventType,
            Payload = payload,
            Status = WebhookDeliveryStatus.Pending
        };
    }
}
