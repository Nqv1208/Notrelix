using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions;

public class Subscription : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public DateTimeOffset CurrentPeriodStart { get; private set; }
    public DateTimeOffset CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }

    private Subscription() : base() { }

    public static Subscription Create(Guid workspaceId, Guid planId, SubscriptionTier tier, DateTimeOffset start, DateTimeOffset end)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(planId);

        var subscription = new Subscription
        {
            WorkspaceId = workspaceId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            Tier = tier,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = end
        };

        subscription.AddDomainEvent(new SubscriptionStartedEvent(workspaceId, planId));
        return subscription;
    }
}
