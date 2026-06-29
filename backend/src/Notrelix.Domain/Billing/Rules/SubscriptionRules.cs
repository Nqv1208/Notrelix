using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Billing.Rules;

public static class SubscriptionRules
{
    public static void EnsureCanChangePlan(SubscriptionStatus currentStatus)
    {
        if (currentStatus is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            throw new BusinessRuleException("Cannot change plan of an inactive subscription.");
    }

    public static void EnsureCanCancel(SubscriptionStatus currentStatus)
    {
        if (currentStatus is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            throw new BusinessRuleException("Subscription is already inactive.");
    }

    public static void EnsurePeriodValid(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            throw new BusinessRuleException("Subscription period start must be before end.");
    }
}
