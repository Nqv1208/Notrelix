namespace Notrelix.Domain.Billing.Plans;

public class PlanPrice : Entity
{
    public Guid PlanId { get; private set; }
    public string Currency { get; private set; } = null!;
    public string BillingInterval { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public bool IsActive { get; private set; } = true;

    private PlanPrice() { }

    public static PlanPrice Create(Guid planId, string currency, string billingInterval, decimal amount)
    {
        Guard.NotEmpty(planId);
        Guard.NotNullOrWhiteSpace(currency);
        Guard.NotNullOrWhiteSpace(billingInterval);
        Guard.NotNegative((double)amount);

        return new PlanPrice
        {
            PlanId = planId,
            Currency = currency.ToUpperInvariant(),
            BillingInterval = billingInterval,
            Amount = amount
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
