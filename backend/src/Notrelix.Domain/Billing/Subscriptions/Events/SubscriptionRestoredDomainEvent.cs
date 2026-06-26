namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionRestoredDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
