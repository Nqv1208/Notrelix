using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-activated")]
public sealed record SubscriptionActivatedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid PlanId { get; }

    public SubscriptionActivatedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid subscriptionId,
        Guid planId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        SubscriptionId = subscriptionId;
        PlanId = planId;
    }
}
