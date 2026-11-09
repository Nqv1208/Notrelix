using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Billing.Rules;

/// <summary>
/// The closed table of canonical subscription status transitions (product BIL-008, section 22). Every
/// status is listed, including Trialing, Unpaid and Incomplete. A status that is not listed as a source
/// has no outgoing transition. Staying in the same status is not a transition and is always allowed here.
/// The aggregate asks this table before it changes <see cref="Subscription.Status"/>.
/// </summary>
public static class SubscriptionStatusTransitions
{
    private static readonly IReadOnlyDictionary<SubscriptionStatus, SubscriptionStatus[]> Allowed =
        new Dictionary<SubscriptionStatus, SubscriptionStatus[]>
        {
            [SubscriptionStatus.Trialing] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled },
            [SubscriptionStatus.Active] = new[] { SubscriptionStatus.PastDue, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.PastDue] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Unpaid] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Incomplete] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Canceled] = Array.Empty<SubscriptionStatus>(),
            [SubscriptionStatus.Expired] = Array.Empty<SubscriptionStatus>(),
        };

    public static bool CanTransition(SubscriptionStatus from, SubscriptionStatus to) =>
        from == to || Allowed[from].Contains(to);

    /// <summary>
    /// Cancel at period end is a flag on an open subscription, not a status. Only Active and PastDue carry it.
    /// </summary>
    public static bool CanScheduleCancellation(SubscriptionStatus current) =>
        current is SubscriptionStatus.Active or SubscriptionStatus.PastDue;
}
