using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Entitlements;

namespace Notrelix.Application.Features.Billing.Entitlements.Services;

/// <summary>
/// Producer-owned implementation of the Billing capability surface. The
/// database-backed entitlement/usage decision lives inside Billing where that
/// commercial policy belongs; consumers only see the stable capability fact.
/// </summary>
public sealed class BillingCapabilityFactsProvider : IBillingCapabilityFacts
{
    private readonly IBillingDbContext _context;
    private readonly IDateTimeProvider _clock;

    public BillingCapabilityFactsProvider(IBillingDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<BillingCapabilityFact?> GetCapabilityAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        int requestedAmount,
        CancellationToken cancellationToken)
    {
        var entitlement = await _context.Entitlements
            .Where(e => e.AccountId == accountId
                && e.Feature.Code == capabilityCode
                && e.Status == EntitlementStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (entitlement is null)
            return new BillingCapabilityFact(IsAvailable: false, Limit: null, Used: null, Remaining: null);

        var now = _clock.UtcNow;
        if (entitlement.ExpiresAt.HasValue && entitlement.ExpiresAt.Value <= now)
            return new BillingCapabilityFact(IsAvailable: false, Limit: null, Used: null, Remaining: null);

        if (entitlement.Limit == 0)
            return new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: null, Remaining: null);

        var used = await _context.FeatureUsageLedger
            .Where(f => f.AccountId == accountId && f.FeatureCode == capabilityCode)
            .SumAsync(f => (decimal?)f.Delta, cancellationToken) ?? 0;

        var usedAmount = (int)used;
        var isAvailable = usedAmount + requestedAmount <= entitlement.Limit;

        return new BillingCapabilityFact(
            IsAvailable: isAvailable,
            Limit: entitlement.Limit,
            Used: usedAmount,
            Remaining: Math.Max(0, entitlement.Limit - usedAmount));
    }
}
