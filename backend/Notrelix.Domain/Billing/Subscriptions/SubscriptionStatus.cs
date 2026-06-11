namespace Notrelix.Domain.Billing.Subscriptions;

public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled,
    Unpaid,
    Incomplete,
    Expired
}
