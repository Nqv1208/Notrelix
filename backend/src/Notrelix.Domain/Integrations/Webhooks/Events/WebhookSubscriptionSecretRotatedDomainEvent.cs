namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionSecretRotatedDomainEvent(
    Guid SubscriptionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
