namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionStartedDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid PlanId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
