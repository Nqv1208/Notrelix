using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-changed")]
public sealed record SubscriptionChangedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    Guid OldPlanId,
    Guid NewPlanId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
