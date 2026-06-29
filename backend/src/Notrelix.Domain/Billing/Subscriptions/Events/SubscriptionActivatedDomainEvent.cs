namespace Notrelix.Domain.Billing.Subscriptions.Events;

public record SubscriptionActivatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid PlanId { get; }

    public SubscriptionActivatedDomainEvent(
        Guid workspaceId,
        Guid subscriptionId,
        Guid planId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SubscriptionId = subscriptionId;
        PlanId = planId;
    }
}
