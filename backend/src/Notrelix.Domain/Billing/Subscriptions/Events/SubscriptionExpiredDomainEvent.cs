namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionExpiredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
