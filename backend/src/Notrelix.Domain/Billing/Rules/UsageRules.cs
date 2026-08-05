namespace Notrelix.Domain.Billing.Rules;

public static class UsageRules
{
    public static void EnsureCanIncrease(int currentUsage, int amount, int limit, bool isHardLimit)
    {
        if (isHardLimit && currentUsage + amount > limit)
        {
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_LimitExceeded, $"Usage limit exceeded. Current: {currentUsage}, Attempted: {amount}, Limit: {limit}");
        }
    }
}
