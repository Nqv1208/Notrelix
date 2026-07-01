namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionStartedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid PlanId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
