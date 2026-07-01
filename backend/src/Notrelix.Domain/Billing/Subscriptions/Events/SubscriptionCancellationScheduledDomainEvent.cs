namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionCancellationScheduledDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
