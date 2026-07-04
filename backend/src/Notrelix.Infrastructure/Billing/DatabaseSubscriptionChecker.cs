using Notrelix.Application.Common.Entitlements;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Infrastructure.Billing;

public sealed class DatabaseSubscriptionChecker : ISubscriptionChecker
{
    private readonly IBillingDbContext _db;

    public DatabaseSubscriptionChecker(IBillingDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await _db.Subscriptions
            .Where(s => s.AccountId == accountId
                && s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd > DateTimeOffset.UtcNow
                && !s.IsDeleted)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> HasMinimumTierAsync(Guid accountId, string minimumTier, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SubscriptionTier>(minimumTier, ignoreCase: true, out var requiredTier))
            return false;

        return await _db.Subscriptions
            .Where(s => s.AccountId == accountId
                && s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd > DateTimeOffset.UtcNow
                && s.Tier >= requiredTier
                && !s.IsDeleted)
            .AnyAsync(cancellationToken);
    }
}
