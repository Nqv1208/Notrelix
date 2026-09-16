namespace Notrelix.Application.Common.Requests.Gates;

/// <summary>
/// Marker for requests that require an active subscription. The access-facts
/// provider resolves the subscription decision through the Billing-owned
/// <c>IBillingSubscriptionFacts</c> seam and composes it into the neutral
/// <c>AccessFacts.SubscriptionRequirementSatisfied</c> fact; the policy engine
/// only consumes that boolean and never interprets tier ordering itself.
/// </summary>
public interface IRequireSubscription
{
    /// <summary>
    /// Minimum subscription tier required (e.g., "Free", "Pro", "Enterprise").
    /// Null means any active subscription is sufficient. This value is passed
    /// verbatim to the Billing producer, which owns the tier comparison.
    /// </summary>
    string? MinimumTier { get; }
}
