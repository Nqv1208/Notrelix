namespace Notrelix.Domain.Integrations.Webhooks;

public enum WebhookDeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Retrying
}
