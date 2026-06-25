namespace Notrelix.Domain.Billing.Rules;

public static class UsageRules
{
    public static void EnsureCanIncrease(int currentUsage, int amount, int limit, bool isHardLimit)
    {
        if (isHardLimit && currentUsage + amount > limit)
        {
            throw new BusinessRuleException($"Usage limit exceeded. Current: {currentUsage}, Attempted: {amount}, Limit: {limit}");
        }
    }
}
