namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionEnabledDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
