using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Application.Features.Billing.Capacity;

/// <summary>
/// Billing-owned hard-capacity action. The WorkspaceFeatureUsage row owns the
/// capacity invariant (optimistic concurrency on the aggregate version makes
/// the last-slot race safe); the ledger records every consume/release effect
/// and provides the LogicalOperationId dedup key. Reads assume the caller runs
/// inside the same request transaction as the quota-bearing resource mutation
/// so the capacity effect commits atomically with the feature change.
/// </summary>
public sealed class BillingCapacityActions : IBillingCapacityActions
{
    private readonly IBillingDbContext _context;
    private readonly IBillingCapabilityFacts _capabilityFacts;

    public BillingCapacityActions(
        IBillingDbContext context,
        IBillingCapabilityFacts capabilityFacts)
    {
        _context = context;
        _capabilityFacts = capabilityFacts;
    }

    public async Task<ConsumeCapacityResult> ConsumeAsync(
        ConsumeCapacityRequest request,
        CancellationToken cancellationToken)
    {
        var op = request.Operation;

        var existing = await _context.FeatureUsageLedger
            .FirstOrDefaultAsync(l =>
                l.AccountId == op.AccountId
                && l.WorkspaceId == op.WorkspaceId
                && l.FeatureCode == op.CapabilityCode
                && l.LogicalOperationId == op.LogicalOperationId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.Delta == op.Amount
                && existing.ReferenceResource == op.SourceResource)
            {
                return new ConsumeCapacityResult(
                    AlreadyConsumed: true,
                    Remaining: await CurrentRemainingAsync(op, cancellationToken));
            }

            throw new CapacityOperationConflictException(op.LogicalOperationId);
        }

        var usage = await LoadOrCreateUsageAsync(op, cancellationToken);

        usage.Consume(op.Amount, op.ActorUserId, op.OccurredAt);

        _context.FeatureUsageLedger.Add(FeatureUsageLedger.Create(
            op.AccountId,
            op.WorkspaceId,
            op.CapabilityCode,
            op.Amount,
            op.ActorUserId,
            op.SourceResource,
            note: "capacity-consumed",
            op.OccurredAt,
            logicalOperationId: op.LogicalOperationId));

        return new ConsumeCapacityResult(AlreadyConsumed: false, Remaining: RemainingOf(usage));
    }

    public async Task<ReleaseCapacityResult> ReleaseAsync(
        ReleaseCapacityRequest request,
        CancellationToken cancellationToken)
    {
        var op = request.Operation;

        var existing = await _context.FeatureUsageLedger
            .FirstOrDefaultAsync(l =>
                l.AccountId == op.AccountId
                && l.WorkspaceId == op.WorkspaceId
                && l.FeatureCode == op.CapabilityCode
                && l.LogicalOperationId == op.LogicalOperationId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.Delta == -op.Amount
                && existing.ReferenceResource == op.SourceResource)
            {
                return new ReleaseCapacityResult(
                    AlreadyReleased: true,
                    Remaining: await CurrentRemainingAsync(op, cancellationToken));
            }

            throw new CapacityOperationConflictException(op.LogicalOperationId);
        }

        var usage = await LoadUsageAsync(op, cancellationToken);
        if (usage is null)
        {
            return new ReleaseCapacityResult(AlreadyReleased: true, Remaining: null);
        }

        usage.Release(op.Amount, op.ActorUserId, op.OccurredAt);

        _context.FeatureUsageLedger.Add(FeatureUsageLedger.Create(
            op.AccountId,
            op.WorkspaceId,
            op.CapabilityCode,
            -op.Amount,
            op.ActorUserId,
            op.SourceResource,
            note: "capacity-released",
            op.OccurredAt,
            logicalOperationId: op.LogicalOperationId));

        return new ReleaseCapacityResult(AlreadyReleased: false, Remaining: RemainingOf(usage));
    }

    /// <summary>
    /// Loads the authoritative per-workspace usage record or seeds it from the
    /// current capability fact. BILL-LIMIT-001 representation: an explicit
    /// unlimited grant materializes as a null hard limit (no ceiling); a finite
    /// limit (including zero) materializes as a numeric ceiling. When no
    /// entitlement exists the record seeds at zero so consumption fails closed.
    /// </summary>
    private async Task<WorkspaceFeatureUsage> LoadOrCreateUsageAsync(
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        var usage = await LoadUsageAsync(op, cancellationToken);
        if (usage is not null)
            return usage;

        var fact = await _capabilityFacts.GetCapabilityAsync(
            op.AccountId,
            op.WorkspaceId,
            op.CapabilityCode,
            requestedAmount: (int)op.Amount,
            cancellationToken);

        // Null limit from the fact means explicit unlimited (unbounded). A
        // missing entitlement (null fact) fails closed to a zero ceiling.
        var effectiveLimit = fact switch
        {
            null => (decimal?)0,
            { Limit: null } => null,
            { Limit: int limit } => limit,
        };

        usage = WorkspaceFeatureUsage.Create(
            op.AccountId,
            op.WorkspaceId,
            FeatureCode.Create(op.CapabilityCode),
            currentUsage: 0,
            hardLimit: effectiveLimit,
            softLimit: effectiveLimit,
            op.OccurredAt);

        _context.WorkspaceFeatureUsages.Add(usage);
        return usage;
    }

    private async Task<WorkspaceFeatureUsage?> LoadUsageAsync(
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        return await _context.WorkspaceFeatureUsages
            .FirstOrDefaultAsync(w =>
                w.AccountId == op.AccountId
                && w.WorkspaceId == op.WorkspaceId
                && w.Feature == FeatureCode.Create(op.CapabilityCode),
                cancellationToken);
    }

    private async Task<decimal?> CurrentRemainingAsync(
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        var usage = await LoadUsageAsync(op, cancellationToken);
        return usage is null ? null : RemainingOf(usage);
    }

    private static decimal? RemainingOf(WorkspaceFeatureUsage usage)
    {
        return usage.HardLimit.HasValue
            ? usage.HardLimit - usage.CurrentUsage
            : null;
    }
}