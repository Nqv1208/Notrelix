using Notrelix.Application.Common.Entitlements;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Billing.Entitlements;

namespace Notrelix.Infrastructure.Billing;

public sealed class DatabaseFeatureGateChecker : IFeatureGateChecker
{
    private readonly IBillingDbContext _db;

    public DatabaseFeatureGateChecker(IBillingDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsFeatureEnabledAsync(Guid accountId, string featureCode, int amount, CancellationToken cancellationToken)
    {
        var entitlement = await _db.Entitlements
            .Where(e => e.AccountId == accountId
                && e.Feature.Code == featureCode
                && e.Status == EntitlementStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (entitlement is null)
            return false;

        if (entitlement.ExpiresAt.HasValue && entitlement.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            return false;

        if (entitlement.Limit == 0)
            return true;

        var totalUsed = await _db.FeatureUsageLedger
            .Where(f => f.AccountId == accountId
                && f.FeatureCode == featureCode)
            .SumAsync(f => (decimal?)f.Delta, cancellationToken) ?? 0;

        return totalUsed + amount <= entitlement.Limit;
    }
}
