namespace Notrelix.Application.Common.Requests.Gates;

/// <summary>
/// Marker for requests that require an active subscription.
/// AccessControlBehavior resolves subscription facts and the policy engine
/// evaluates this requirement before handler execution.
/// </summary>
public interface IRequireSubscription
{
    /// <summary>
    /// Minimum subscription tier required (e.g., "Free", "Pro", "Enterprise").
    /// Null means any active subscription is sufficient.
    /// </summary>
    string? MinimumTier { get; }
}
