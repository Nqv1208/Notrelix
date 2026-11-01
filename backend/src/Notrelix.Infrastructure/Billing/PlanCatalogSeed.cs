using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Billing;

/// <summary>
/// The baseline plan catalog every environment starts with: revision 1 of the free, pro and enterprise families.
/// It is data, written once and only when a family has no revision at all; the platform operator edits the
/// catalog by revision afterwards, and a later run never touches what the operator published. The fixed ids are
/// the identities subscriptions referenced before the catalog moved into the database.
/// </summary>
public static class PlanCatalogSeed
{
    public static readonly Guid FreeId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000001");
    public static readonly Guid ProId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000002");
    public static readonly Guid EnterpriseId = Guid.Parse("0b5f0f1e-7a3c-7c10-8a00-000000000003");

    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record Baseline(Guid Id, string Family, string Name, SubscriptionTier Tier, (string Code, int? Limit)[] Limits);

    private static readonly Baseline[] Baselines =
    [
        new(FreeId, "free", "Free", SubscriptionTier.Free,
        [
            (BillingCapabilityCode.AutomationRule, 5), (BillingCapabilityCode.MemberSeat, 10),
            (BillingCapabilityCode.StorageGb, 2), (BillingCapabilityCode.GuestSeat, 5),
        ]),
        new(ProId, "pro", "Pro", SubscriptionTier.Pro,
        [
            (BillingCapabilityCode.AutomationRule, 100), (BillingCapabilityCode.MemberSeat, 500),
            (BillingCapabilityCode.StorageGb, 100), (BillingCapabilityCode.GuestSeat, 200),
        ]),
        new(EnterpriseId, "enterprise", "Enterprise", SubscriptionTier.Enterprise,
        [
            (BillingCapabilityCode.AutomationRule, null), (BillingCapabilityCode.MemberSeat, null),
            (BillingCapabilityCode.StorageGb, 1000), (BillingCapabilityCode.GuestSeat, null),
            (BillingCapabilityCode.CustomRoles, null),
        ]),
    ];

    /// <summary>A development subscription of an account on the baseline Pro plan (seed data only).</summary>
    public static Subscription CreateProSubscription(Guid accountId, DateTimeOffset start, DateTimeOffset end) =>
        Subscription.Create(accountId, ProId, SubscriptionTier.Pro, start, end, createdBy: null, start);

    /// <summary>Inserts the baseline revision of each family that has no revision yet. Runs on the owner connection.</summary>
    public static async Task EnsureAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var present = await context.Plans.AsNoTracking().Select(p => p.FamilyKey).Distinct().ToListAsync(cancellationToken);
        var added = false;
        foreach (var baseline in Baselines.Where(b => !present.Contains(b.Family)))
        {
            var plan = Plan.Create(
                baseline.Family, 1, baseline.Name, baseline.Tier, Money.Create(0, "VND"), BillingPeriod.Monthly, Epoch, baseline.Id);
            foreach (var (code, limit) in baseline.Limits)
                plan.AddLimit(FeatureCode.Create(code), limit ?? 0, Epoch, isUnlimited: limit is null);
            context.Plans.Add(plan);
            added = true;
        }

        if (!added)
            return;

        // The baseline is data, not a business event: it raises no integration message.
        foreach (var entry in context.ChangeTracker.Entries<Entity>().Select(e => e.Entity).OfType<IHasDomainEvents>())
            entry.ClearDomainEvents();
        await context.SaveChangesAsync(cancellationToken);
    }
}
