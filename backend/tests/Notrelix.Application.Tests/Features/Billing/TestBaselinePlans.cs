using Notrelix.Domain.Common;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Tests.Features.Billing;

/// <summary>The plan catalog rows the unit tests read, built through the Plan aggregate like the platform baseline.</summary>
internal static class TestBaselinePlans
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Plan Free => Build("free", "Free", SubscriptionTier.Free, 1, FreeId,
        (BillingCapabilityCode.AutomationRule, 5), (BillingCapabilityCode.MemberSeat, 10),
        (BillingCapabilityCode.StorageGb, 2), (BillingCapabilityCode.GuestSeat, 5));

    public static Plan Pro => Build("pro", "Pro", SubscriptionTier.Pro, 1, ProId,
        (BillingCapabilityCode.AutomationRule, 100), (BillingCapabilityCode.MemberSeat, 500),
        (BillingCapabilityCode.StorageGb, 100), (BillingCapabilityCode.GuestSeat, 200));

    public static Plan Enterprise => Build("enterprise", "Enterprise", SubscriptionTier.Enterprise, 1, EnterpriseId,
        (BillingCapabilityCode.AutomationRule, null), (BillingCapabilityCode.MemberSeat, null),
        (BillingCapabilityCode.StorageGb, 1000), (BillingCapabilityCode.GuestSeat, null),
        (BillingCapabilityCode.CustomRoles, null));

    public static readonly Guid FreeId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000001");
    public static readonly Guid ProId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000002");
    public static readonly Guid EnterpriseId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000003");

    /// <summary>Every access builds fresh rows (stable ids), so no test sees another test's mutation.</summary>
    public static List<Plan> All() => [Free, Pro, Enterprise];

    public static Plan Build(string family, string name, SubscriptionTier tier, int revision, params (string Code, int? Limit)[] limits) =>
        Build(family, name, tier, revision, null, limits);

    public static Plan Build(string family, string name, SubscriptionTier tier, int revision, Guid? id, params (string Code, int? Limit)[] limits)
    {
        var plan = Plan.Create(family, revision, name, tier, Money.Create(0, "VND"), BillingPeriod.Monthly, Epoch, id);
        foreach (var (code, limit) in limits)
            plan.AddLimit(FeatureCode.Create(code), limit ?? 0, Epoch, isUnlimited: limit is null);
        ((IHasDomainEvents)plan).ClearDomainEvents();
        return plan;
    }
}
