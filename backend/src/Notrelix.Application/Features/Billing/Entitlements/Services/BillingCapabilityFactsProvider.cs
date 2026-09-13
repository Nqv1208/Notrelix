using Notrelix.Application.Features.Billing.Abstractions;
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
        var now = _clock.UtcNow;
        var entitlement = await ResolveApplicableEntitlementAsync(
            accountId, workspaceId, capabilityCode, now, cancellationToken);

        // Used authority: the ledger sum for the (account, workspace, capability)
        // scope in EVERY state — finite, zero, unlimited, or missing. History is
        // never lossy: a zero or missing grant still reports the actual ledger sum.
        var used = await _context.FeatureUsageLedger
            .Where(f => f.AccountId == accountId
                && f.WorkspaceId == workspaceId
                && f.FeatureCode == capabilityCode)
            .SumAsync(f => (decimal?)f.Delta, cancellationToken) ?? 0;
        var usedAmount = (int)used;

        if (entitlement is null)
            return new BillingCapabilityFact(IsAvailable: false, Limit: null, Used: usedAmount, Remaining: null);

        // BILL-LIMIT-001: explicit unlimited is quantified differently from a
        // zero numeric limit. Zero capacity and unlimited are distinct; only the
        // IsUnlimited representation grants unbounded access.
        if (entitlement.IsUnlimited)
            return new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: usedAmount, Remaining: null);

        if (entitlement.Limit == 0)
            return new BillingCapabilityFact(IsAvailable: false, Limit: 0, Used: usedAmount, Remaining: 0);

        var isAvailable = usedAmount + requestedAmount <= entitlement.Limit;

        return new BillingCapabilityFact(
            IsAvailable: isAvailable,
            Limit: entitlement.Limit,
            Used: usedAmount,
            Remaining: Math.Max(0, entitlement.Limit - usedAmount));
    }

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
