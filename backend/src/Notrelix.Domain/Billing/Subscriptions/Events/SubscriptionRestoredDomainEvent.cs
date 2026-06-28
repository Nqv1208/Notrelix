namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionRestoredDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
