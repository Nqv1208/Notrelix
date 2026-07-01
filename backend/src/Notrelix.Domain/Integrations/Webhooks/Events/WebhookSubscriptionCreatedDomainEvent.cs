namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionCreatedDomainEvent(
    Guid AccountId,
    Guid SubscriptionId,
    Guid WorkspaceId,
    string TargetUrl,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
