using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-canceled")]
public sealed record SubscriptionCanceledDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid SubscriptionId { get; }

    public SubscriptionCanceledDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid subscriptionId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        SubscriptionId = subscriptionId;
    }
}
