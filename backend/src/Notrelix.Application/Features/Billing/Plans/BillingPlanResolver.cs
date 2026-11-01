using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Plans;

/// <summary>
/// Resolves the plan an account is on: the revision the newest active account-level subscription
/// references while it is inside its period, otherwise the platform default plan.
/// </summary>
public sealed class BillingPlanResolver
{
    private readonly IBillingDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly BillingPlanCatalogReader _catalog;

    public BillingPlanResolver(IBillingDbContext context, IDateTimeProvider clock, BillingPlanCatalogReader catalog)
    {
        _context = context;
        _clock = clock;
        _catalog = catalog;
    }

    public async Task<BillingPlanDefinition> ResolveAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var planIds = await _context.Subscriptions
            .Where(s => s.AccountId == accountId
                && s.WorkspaceId == null
                && s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.PlanId)
            .Take(1)
            .ToListAsync(cancellationToken);

        if (planIds.Count == 1 && await _catalog.FindByIdAsync(planIds[0], cancellationToken) is { } subscribed)
            return subscribed;

        return await _catalog.GetDefaultAsync(cancellationToken);
    }
}
