namespace Notrelix.Domain.Integrations.Webhooks.Events;

[EventName("integrations.webhook-subscription-disabled")]
public sealed record WebhookSubscriptionDisabledDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
