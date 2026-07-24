using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-started")]
public sealed record SubscriptionStartedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    Guid PlanId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
