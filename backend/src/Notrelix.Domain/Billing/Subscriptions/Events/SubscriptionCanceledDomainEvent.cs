namespace Notrelix.Domain.Billing.Subscriptions.Events;

public record SubscriptionCanceledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SubscriptionId { get; }

    public SubscriptionCanceledDomainEvent(
        Guid workspaceId,
        Guid subscriptionId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SubscriptionId = subscriptionId;
    }
}
