namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionDisabledDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
