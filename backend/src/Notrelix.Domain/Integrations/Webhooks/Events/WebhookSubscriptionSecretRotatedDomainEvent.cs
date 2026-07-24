namespace Notrelix.Domain.Integrations.Webhooks.Events;

[EventName("integrations.webhook-subscription-secret-rotated")]
public sealed record WebhookSubscriptionSecretRotatedDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
