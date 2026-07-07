namespace Notrelix.Application.Common.Requests;

/// <summary>
/// Marker for requests that require an active subscription.
/// SubscriptionGateBehavior checks subscription status before handler executes.
/// </summary>
public interface IRequireSubscription
{
    /// <summary>
    /// Minimum subscription tier required (e.g., "Free", "Pro", "Enterprise").
    /// Null means any active subscription is sufficient.
    /// </summary>
    string? MinimumTier { get; }
}
