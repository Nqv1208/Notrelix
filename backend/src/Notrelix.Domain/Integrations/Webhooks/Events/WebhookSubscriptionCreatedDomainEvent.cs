namespace Notrelix.Domain.Integrations.Webhooks.Events;

[EventName("integrations.webhook-subscription-created")]
public sealed record WebhookSubscriptionCreatedDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    string TargetUrl,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
