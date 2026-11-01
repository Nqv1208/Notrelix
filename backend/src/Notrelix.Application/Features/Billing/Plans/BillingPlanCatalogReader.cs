using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Application.Features.Billing.Plans;

/// <summary>
/// Reads the plan catalog from the database. The catalog is data the platform operator edits by revision;
/// nothing in code names a plan, a price or a limit. The default plan is the newest active revision of the
/// <see cref="DefaultFamily"/> family: an account without an active subscription is on it.
/// </summary>
public sealed class BillingPlanCatalogReader
{
    public const string DefaultFamily = "free";

    private readonly IBillingDbContext _context;

    public BillingPlanCatalogReader(IBillingDbContext context) => _context = context;

    /// <summary>The newest active revision of every family: what an account may be moved to.</summary>
    public async Task<IReadOnlyList<BillingPlanDefinition>> ListSellableAsync(CancellationToken cancellationToken)
    {
        var plans = await _context.Plans.AsNoTracking().Include(p => p.Limits)
            .Where(p => p.Status == PlanStatus.Active).ToListAsync(cancellationToken);
        return plans.GroupBy(p => p.FamilyKey)
            .Select(g => g.OrderByDescending(p => p.Revision).First())
            .OrderBy(p => p.Tier).ThenBy(p => p.Price.Amount).ThenBy(p => p.FamilyKey, StringComparer.Ordinal)
            .Select(ToDefinition).ToList();
    }

    /// <summary>A revision by identity, whatever its status: a subscription keeps resolving to an archived revision.</summary>
    public async Task<BillingPlanDefinition?> FindByIdAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans.AsNoTracking().Include(p => p.Limits)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        return plan is null ? null : ToDefinition(plan);
    }

    /// <summary>The platform default (Free): the newest active revision of the default family.</summary>
    public async Task<BillingPlanDefinition> GetDefaultAsync(CancellationToken cancellationToken)
    {
        var plan = await _context.Plans.AsNoTracking().Include(p => p.Limits)
            .Where(p => p.FamilyKey == DefaultFamily && p.Status == PlanStatus.Active)
            .OrderByDescending(p => p.Revision).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"The plan catalog has no active '{DefaultFamily}' plan: the platform default is missing.");
        return ToDefinition(plan);
    }

    internal static BillingPlanDefinition ToDefinition(Plan plan) => new(
        plan.FamilyKey,
        plan.Revision,
        plan.Name,
        plan.Tier,
        plan.Id,
        plan.Limits.ToDictionary(l => l.Feature.Code, l => l.IsUnlimited ? (int?)null : l.Limit));
}
