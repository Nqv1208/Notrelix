using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Public.Subscription;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Subscriptions.Services;

/// <summary>
/// Producer-owned implementation of the Billing subscription surface.
/// The database-backed commercial decision — active subscription combined
/// with tier ordering — lives here, in Billing, where that policy belongs.
/// Consumers see only the neutral <c>bool</c>. The <see cref="SubscriptionTier"/>
/// enum is the sole authority for tier ordering across the codebase.
/// </summary>
public sealed class BillingSubscriptionFactsProvider : IBillingSubscriptionFacts
{
    private readonly IBillingDbContext _context;
    private readonly IDateTimeProvider _clock;

    public BillingSubscriptionFactsProvider(IBillingDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<bool> SatisfiesRequirementAsync(
        Guid accountId,
        string? minimumTier,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var activeTiers = await _context.Subscriptions
            .Where(s => s.AccountId == accountId
                && s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd > now)
            .Select(s => s.Tier)
            .ToListAsync(cancellationToken);

        if (activeTiers.Count == 0)
            return false;

        // Any active subscription suffices when no minimum tier is required.
        if (string.IsNullOrEmpty(minimumTier))
            return true;

        // Unknown tier fails closed — no implicit grant.
        if (!Enum.TryParse<SubscriptionTier>(minimumTier, ignoreCase: true, out var requiredTier))
            return false;

        // Tier ordering is defined by the Billing-owned Domain enum alone.
        return activeTiers.Any(t => t >= requiredTier);
    }
}
