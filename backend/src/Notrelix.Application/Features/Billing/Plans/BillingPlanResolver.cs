using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Plans;

/// <summary>
/// Resolves the plan an account is on: the newest active account-level
/// subscription that is still inside its period, otherwise Free.
/// </summary>
public sealed class BillingPlanResolver
{
    private readonly IBillingDbContext _context;
    private readonly IDateTimeProvider _clock;

    public BillingPlanResolver(IBillingDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<BillingPlanDefinition> ResolveAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var tiers = await _context.Subscriptions
            .Where(s => s.AccountId == accountId
                && s.WorkspaceId == null
                && s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.Tier)
            .Take(1)
            .ToListAsync(cancellationToken);

        return tiers.Count == 0 ? BillingPlanCatalog.Free : BillingPlanCatalog.ForTier(tiers[0]);
    }
}
