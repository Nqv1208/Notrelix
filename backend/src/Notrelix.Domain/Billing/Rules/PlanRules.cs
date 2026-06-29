namespace Notrelix.Domain.Billing.Rules;

public static class PlanRules
{
    public static void EnsureNameNotTooLong(string name, int maxLength = 100)
    {
        Guard.MaxLength(name, maxLength);
    }

    public static void EnsurePricePositive(Money price)
    {
        if (price.Amount < 0)
            throw new BusinessRuleException("Plan price cannot be negative.");
    }
}
