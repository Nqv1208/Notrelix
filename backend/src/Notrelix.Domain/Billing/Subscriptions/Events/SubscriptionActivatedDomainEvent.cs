namespace Notrelix.Domain.Billing.Subscriptions.Events;

public record SubscriptionActivatedDomainEvent : BillingAccountScopedDomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid PlanId { get; }

    public SubscriptionActivatedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        Guid subscriptionId,
        Guid planId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt, actorUserId)
    {
        SubscriptionId = subscriptionId;
        PlanId = planId;
    }
}
