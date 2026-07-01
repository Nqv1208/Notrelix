namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
