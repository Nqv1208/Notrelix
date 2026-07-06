namespace Notrelix.Domain.Billing.Subscriptions;

public class SubscriptionItem : Entity
{
    public Guid SubscriptionId { get; private set; }
    public Guid PlanPriceId { get; private set; }
    public int Quantity { get; private set; }

    private SubscriptionItem() { }

    public static SubscriptionItem Create(Guid subscriptionId, Guid planPriceId, int quantity)
    {
        Guard.NotEmpty(subscriptionId);
        Guard.NotEmpty(planPriceId);
        Guard.Assert(quantity > 0, "Quantity must be positive.");

        return new SubscriptionItem
        {
            SubscriptionId = subscriptionId,
            PlanPriceId = planPriceId,
            Quantity = quantity
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Guard.Assert(quantity > 0, "Quantity must be positive.");
        Quantity = quantity;
    }
}
