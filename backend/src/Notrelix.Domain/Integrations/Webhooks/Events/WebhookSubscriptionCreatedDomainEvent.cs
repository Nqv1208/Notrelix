namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionCreatedDomainEvent(
    Guid SubscriptionId,
    Guid WorkspaceId,
    string TargetUrl,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
