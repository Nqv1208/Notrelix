using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Plans;

/// <summary>
/// One commercial plan of the code-defined catalog. A limit of null means the
/// capability is unlimited on that plan. <see cref="PlanId"/> is a stable
/// identity so a subscription can reference the plan without a stored copy.
/// </summary>
public sealed record BillingPlanDefinition(
    string Code,
    string Name,
    SubscriptionTier Tier,
    Guid PlanId,
    IReadOnlyDictionary<string, int?> Limits)
{
    /// <summary>The limit of a capability on this plan; false when the plan does not define it.</summary>
    public bool TryGetLimit(string capabilityCode, out int? limit)
        => Limits.TryGetValue(capabilityCode, out limit);
}

/// <summary>
/// The Billing-owned plan catalog (Free, Pro, Enterprise). Plan limits are the
/// base entitlement of an account: an explicit Entitlement grant still wins over
/// the plan, and an account without an active subscription is on Free.
/// </summary>
public static class BillingPlanCatalog
{
    public static readonly BillingPlanDefinition Free = new(
        "free", "Free", SubscriptionTier.Free,
        Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000001"),
        new Dictionary<string, int?>
        {
            [BillingCapabilityCode.AutomationRule] = 5,
            [BillingCapabilityCode.MemberSeat] = 10,
            [BillingCapabilityCode.StorageGb] = 2,
            [BillingCapabilityCode.GuestSeat] = 5
        });

    public static readonly BillingPlanDefinition Pro = new(
        "pro", "Pro", SubscriptionTier.Pro,
        Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000002"),
        new Dictionary<string, int?>
        {
            [BillingCapabilityCode.AutomationRule] = 100,
            [BillingCapabilityCode.MemberSeat] = 500,
            [BillingCapabilityCode.StorageGb] = 100,
            [BillingCapabilityCode.GuestSeat] = 200
        });

    public static readonly BillingPlanDefinition Enterprise = new(
        "enterprise", "Enterprise", SubscriptionTier.Enterprise,
        Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000003"),
        new Dictionary<string, int?>
        {
            [BillingCapabilityCode.AutomationRule] = null,
            [BillingCapabilityCode.MemberSeat] = null,
            [BillingCapabilityCode.StorageGb] = 1000,
            [BillingCapabilityCode.GuestSeat] = null,
            [BillingCapabilityCode.CustomRoles] = null
        });

    public static readonly IReadOnlyList<BillingPlanDefinition> All = [Free, Pro, Enterprise];

    public static BillingPlanDefinition ForTier(SubscriptionTier tier)
        => All.First(p => p.Tier == tier);

    public static BillingPlanDefinition? FindByCode(string? code)
        => All.FirstOrDefault(p => string.Equals(p.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));
}
