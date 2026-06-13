using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Events;

public record SubscriptionCanceledDomainEvent : DomainEvent
{
    public Guid SubscriptionId { get; }

    public SubscriptionCanceledDomainEvent(
        Guid workspaceId,
        Guid subscriptionId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        SubscriptionId = subscriptionId;
    }
}
