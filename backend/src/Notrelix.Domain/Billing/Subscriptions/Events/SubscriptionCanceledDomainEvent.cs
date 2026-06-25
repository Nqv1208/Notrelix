namespace Notrelix.Domain.Billing.Subscriptions.Events;

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
