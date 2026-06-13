using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record SubscriptionActivatedDomainEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid PlanId { get; }

    public SubscriptionActivatedDomainEvent(
        Guid workspaceId,
        Guid subscriptionId,
        Guid planId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        SubscriptionId = subscriptionId;
        PlanId = planId;
    }
}
