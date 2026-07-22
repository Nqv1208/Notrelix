namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionPastDueDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
