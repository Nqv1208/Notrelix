namespace Notrelix.Domain.Integrations.Webhooks.Events;

[EventName("integrations.webhook-delivery-recorded")]
public sealed record WebhookDeliveryRecordedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeliveryId,
    WebhookDeliveryStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
