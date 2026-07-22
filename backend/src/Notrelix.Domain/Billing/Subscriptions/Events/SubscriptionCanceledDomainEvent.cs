namespace Notrelix.Domain.Billing.Subscriptions.Events;

public record SubscriptionCanceledDomainEvent : BillingAccountScopedDomainEvent
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
