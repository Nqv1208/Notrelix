namespace Notrelix.Domain.Integrations.Webhooks.Events;

public class InboundWebhookEvent : AggregateRoot
{
    public Guid? WorkspaceId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string? ExternalEventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public JsonValue Payload { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }

    private InboundWebhookEvent() : base() { }

    public static InboundWebhookEvent Record(string provider, string eventType, JsonValue payload, DateTimeOffset receivedAt, Guid? workspaceId = null, string? externalId = null)
    {
        return new InboundWebhookEvent
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            EventType = eventType,
            Payload = payload,
            ExternalEventId = externalId,
            ReceivedAt = receivedAt
        };
    }
}
