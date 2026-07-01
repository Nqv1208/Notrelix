namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
