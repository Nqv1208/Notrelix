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
/// and provides the global LogicalOperationId dedup identity. Read/execute
/// must run inside the caller's request transaction (the data session) so the
/// capacity effect commits atomically with the quota-bearing resource mutation.
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

        // LogicalOperationId is global identity: a replay of an executed
        // operation is detected by that id alone, whatever the scope. An
        // identical semantic payload replays; anything else (including another
        // scope) is a deterministic conflict.
        var existing = await FindByIdentityAsync(op.LogicalOperationId, cancellationToken);
        if (existing is not null)
        {
            if (SamePayload(existing, op, expectedDelta: op.Amount))
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

        var existing = await FindByIdentityAsync(op.LogicalOperationId, cancellationToken);
        if (existing is not null)
        {
            if (SamePayload(existing, op, expectedDelta: -op.Amount))
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

        await ReconcileLimitsAsync(usage, op, cancellationToken);
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
    /// current capability fact through the atomic first-use primitive. On every
    /// path the effective limit is reconciled with the current fact: a missing
    /// grant or an unavailable null-limit grant fails closed to a zero ceiling,
    /// an explicit unlimited grant clears the ceiling, and a finite (including
    /// zero) limit reconciles to its numeric value regardless of whether the
    /// current request is available. Reconfiguring never deletes committed
    /// usage; a downgrade below it denies new consumption instead.
    /// </summary>
    private async Task<WorkspaceFeatureUsage> LoadOrCreateUsageAsync(
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        var existing = await LoadUsageAsync(op, cancellationToken);
        if (existing is not null)
        {
            await ReconcileLimitsAsync(existing, op, cancellationToken);
            return existing;
        }

        var (hardLimit, softLimit) = await EffectiveLimitAsync(op, cancellationToken);
        var usage = await _context.GetOrCreateWorkspaceFeatureUsageAsync(
            op.AccountId,
            op.WorkspaceId,
            op.CapabilityCode,
            hardLimit,
            softLimit,
            op.ActorUserId,
            op.OccurredAt,
            cancellationToken);

        // The primitive may have lost a concurrent first-use race and returned
        // the winner's row, so reconciliation must still resolve our fact.
        ReconcileLimits(usage, hardLimit, softLimit, op.ActorUserId, op.OccurredAt);
        return usage;
    }

    private async Task ReconcileLimitsAsync(
        WorkspaceFeatureUsage usage,
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        var (hardLimit, softLimit) = await EffectiveLimitAsync(op, cancellationToken);
        ReconcileLimits(usage, hardLimit, softLimit, op.ActorUserId, op.OccurredAt);
    }

    private static void ReconcileLimits(
        WorkspaceFeatureUsage usage,
        decimal? hardLimit,
        decimal? softLimit,
        Guid actorUserId,
        DateTimeOffset occurredAt)
        => usage.ReconfigureLimits(hardLimit, softLimit, actorUserId, occurredAt);

    private async Task<(decimal? HardLimit, decimal? SoftLimit)> EffectiveLimitAsync(
        BillingCapacityOperationIdentity op,
        CancellationToken cancellationToken)
    {
        var fact = await _capabilityFacts.GetCapabilityAsync(
            op.AccountId,
            op.WorkspaceId,
            op.CapabilityCode,
            requestedAmount: (int)op.Amount,
            cancellationToken);

        // A finite grant is the ceiling regardless of its availability: the
        // request-specific IsAvailable may be false only because Used already
        // equals the Limit (the requested amount no longer fits), which must
        // never shrink the persisted ceiling. IsAvailable only distinguishes
        // the null-limit facts: an available null grant is unbounded, an
        // unavailable null grant or a missing fact fails closed to zero.
        var effective = fact switch
        {
            null => (decimal?)0,
            { Limit: int limit } => limit,
            { IsAvailable: true, Limit: null } => null,
            { IsAvailable: false, Limit: null } => 0,
        };

        return (effective, effective);
    }

    private async Task<FeatureUsageLedger?> FindByIdentityAsync(
        Guid logicalOperationId,
        CancellationToken cancellationToken)
        => await _context.FeatureUsageLedger
            .FirstOrDefaultAsync(l => l.LogicalOperationId == logicalOperationId, cancellationToken);

    private static bool SamePayload(
        FeatureUsageLedger existing,
        BillingCapacityOperationIdentity op,
        decimal expectedDelta)
        => existing.AccountId == op.AccountId
            && existing.WorkspaceId == op.WorkspaceId
            && existing.FeatureCode == NormalizeCode(op.CapabilityCode)
            && existing.Delta == expectedDelta
            && existing.ReferenceResource == op.SourceResource;

    private static string NormalizeCode(string capabilityCode)
        => capabilityCode.Trim().ToUpperInvariant();

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
            ? Math.Max(0m, usage.HardLimit.Value - usage.CurrentUsage)
            : null;
    }
}