namespace Notrelix.Domain.Integrations.Webhooks.Events;

[EventName("integrations.webhook-subscription-enabled")]
public sealed record WebhookSubscriptionEnabledDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
