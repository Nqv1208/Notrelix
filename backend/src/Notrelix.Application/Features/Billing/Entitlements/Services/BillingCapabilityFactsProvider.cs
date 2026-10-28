using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

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
    private readonly IBillingUsageReader _usageReader;
    private readonly BillingPlanResolver _planResolver;

    public BillingCapabilityFactsProvider(
        IBillingDbContext context,
        IDateTimeProvider clock,
        IBillingUsageReader usageReader,
        BillingPlanResolver planResolver)
    {
        _context = context;
        _clock = clock;
        _usageReader = usageReader;
        _planResolver = planResolver;
    }

    public async Task<BillingCapabilityFact?> GetCapabilityAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        int requestedAmount,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var entitlement = await ResolveApplicableEntitlementAsync(
            accountId, workspaceId, capabilityCode, now, cancellationToken);

        var usedAmount = await UsedAsync(accountId, workspaceId, capabilityCode, cancellationToken);

        if (entitlement is null)
        {
            // No explicit grant: the plan the account is on is the base entitlement.
            var plan = await _planResolver.ResolveAsync(accountId, cancellationToken);
            if (!plan.TryGetLimit(capabilityCode.Trim().ToUpperInvariant(), out var planLimit))
                return new BillingCapabilityFact(IsAvailable: false, Limit: null, Used: usedAmount, Remaining: null);

            if (planLimit is null)
                return new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: usedAmount, Remaining: null);

            return Finite(planLimit.Value, usedAmount, requestedAmount);
        }

        // BILL-LIMIT-001: explicit unlimited is quantified differently from a
        // zero numeric limit. Zero capacity and unlimited are distinct; only the
        // IsUnlimited representation grants unbounded access.
        if (entitlement.IsUnlimited)
            return new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: usedAmount, Remaining: null);

        if (entitlement.Limit == 0)
            return new BillingCapabilityFact(IsAvailable: false, Limit: 0, Used: usedAmount, Remaining: 0);

        return Finite(entitlement.Limit, usedAmount, requestedAmount);
    }

    private static BillingCapabilityFact Finite(int limit, int usedAmount, int requestedAmount)
        => new(
            IsAvailable: usedAmount + requestedAmount <= limit,
            Limit: limit,
            Used: usedAmount,
            Remaining: Math.Max(0, limit - usedAmount));

    /// <summary>
    /// Used authority per capability. Automation rules are metered by the usage
    /// ledger (every state, summed across the account's workspaces so the limit is
    /// account-wide); seats and storage are counted live across the account.
    /// Storage is reported in whole GB, rounded up.
    /// </summary>
    private async Task<int> UsedAsync(
        Guid accountId, Guid workspaceId, string capabilityCode, CancellationToken cancellationToken)
    {
        switch (capabilityCode.Trim().ToUpperInvariant())
        {
            case BillingCapabilityCode.MemberSeat:
                return (await _usageReader.GetAccountUsageAsync(accountId, workspaceId, cancellationToken)).MemberSeats;
            case BillingCapabilityCode.GuestSeat:
                return (await _usageReader.GetAccountUsageAsync(accountId, workspaceId, cancellationToken)).GuestSeats;
            case BillingCapabilityCode.StorageGb:
                var usage = await _usageReader.GetAccountUsageAsync(accountId, workspaceId, cancellationToken);
                return (int)Math.Ceiling(usage.StorageBytes / (double)BytesPerGb);
            default:
                var ledger = await _context.FeatureUsageLedger
                    .Where(f => f.AccountId == accountId && f.FeatureCode == capabilityCode)
                    .SumAsync(f => (decimal?)f.Delta, cancellationToken) ?? 0;
                return (int)ledger;
        }
    }

    public const long BytesPerGb = 1024L * 1024 * 1024;

    /// <summary>
    /// Resolves the single applicable entitlement for an Account/Workspace/
    /// capability combination. The applicable set is reduced BEFORE any
    /// ordering/Take(1): status must be Active and expiry must be in the future,
    /// so an expired Workspace grant can never shadow a valid Account grant.
    /// Within the applicable set a workspace-targeted entitlement wins over an
    /// account-scoped entitlement; within the same scope the newest grant wins
    /// (CreatedAt descending, then Id) so the choice is deterministic and never
    /// an arbitrary first row. TAC-BI-001 pins this precedence rule. Feature is
    /// a converter-mapped value object, so translation relies on value-object
    /// equality (e.Feature == FeatureCode.Create(...)) rather than member access.
    /// </summary>
    private async Task<Entitlement?> ResolveApplicableEntitlementAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var candidates = await _context.Entitlements
            .Where(e => e.AccountId == accountId
                && e.Feature == FeatureCode.Create(capabilityCode)
                && e.Status == EntitlementStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > now)
                && (e.TargetScope == EntitlementTargetScope.Account
                    || (e.TargetScope == EntitlementTargetScope.Workspace && e.TargetWorkspaceId == workspaceId)))
            .OrderBy(e => e.TargetScope == EntitlementTargetScope.Workspace ? 0 : 1)
            .ThenByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(1)
            .ToListAsync(cancellationToken);

        return candidates.SingleOrDefault();
    }
}
