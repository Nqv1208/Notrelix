namespace Notrelix.Application.Common.Requests.Gates;

/// <summary>
/// Marker for requests that require a specific feature to be enabled.
/// The AccessPolicyEngine evaluates feature entitlement from AccessFacts.
/// </summary>
public interface IRequireFeature
{
    /// <summary>
    /// The feature code required (e.g., "automation", "advanced_fields", "export").
    /// </summary>
    string FeatureCode { get; }

    /// <summary>
    /// Amount of feature usage to consume (e.g., 1 for single use).
    /// Zero means check-only, no usage consumed.
    /// </summary>
    int Amount { get; }
}
