using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Plans;

/// <summary>
/// One revision of a plan family as the catalog stores it. A limit of null means the capability is unlimited
/// on that plan. <see cref="PlanId"/> identifies the revision a subscription references, so a subscription keeps
/// its revision when a newer one is published.
/// </summary>
public sealed record BillingPlanDefinition(
    string Code,
    int Revision,
    string Name,
    SubscriptionTier Tier,
    Guid PlanId,
    IReadOnlyDictionary<string, int?> Limits)
{
    /// <summary>The limit of a capability on this plan; false when the plan does not define it.</summary>
    public bool TryGetLimit(string capabilityCode, out int? limit)
        => Limits.TryGetValue(capabilityCode, out limit);
}
