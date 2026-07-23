using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Billing.Rules;

public static class SubscriptionRules
{
    public static void EnsureCanChangePlan(SubscriptionStatus currentStatus)
    {
        if (currentStatus is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_CannotChangePlanOfInactive, "Cannot change plan of an inactive subscription.");
    }

    public static void EnsureCanCancel(SubscriptionStatus currentStatus)
    {
        if (currentStatus is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_AlreadyInactive, "Subscription is already inactive.");
    }

    public static void EnsurePeriodValid(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_PeriodStartMustBeBeforeEnd, "Subscription period start must be before end.");
    }
}
