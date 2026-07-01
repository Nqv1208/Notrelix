namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookDeliveryRecordedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeliveryId,
    WebhookDeliveryStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
