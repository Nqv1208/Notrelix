namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid OldPlanId,
    Guid NewPlanId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
