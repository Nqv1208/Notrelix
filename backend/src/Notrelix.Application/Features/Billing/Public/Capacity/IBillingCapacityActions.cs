namespace Notrelix.Application.Features.Billing.Public.Capacity;

/// <summary>
/// Caller-owned operation identity for a Billing capacity action.
/// Carries the Account/Workspace scope, the capability, the amount, the
/// logical operation (producer dedup) identity, the affected source resource
/// and the explicit executor/event facts supplied by the caller.
/// Ownership statement:
/// <para>Billing owns the capacity invariant (WorkspaceFeatureUsage + ledger).</para>
/// <para>The producer needs a hard-capacity slot before a quota-bearing resource
/// (e.g. an AutomationRule) can be created.</para>
/// <para>The producer obtains it by invoking this Billing-owned action with this
/// identity; it never reads or mutates Billing storage directly.</para>
/// </summary>
public sealed record BillingCapacityOperationIdentity(
    Guid AccountId,
    Guid WorkspaceId,
    string CapabilityCode,
    decimal Amount,
    Guid LogicalOperationId,
    string SourceResource,
    Guid ActorUserId,
    DateTimeOffset OccurredAt);

/// <summary>
/// Producer-owned consume request. Retry with the same LogicalOperationId and
/// the same payload replays one usage effect; a conflicting payload on a
/// consumed operation fails deterministically.
/// </summary>
public sealed record ConsumeCapacityRequest(BillingCapacityOperationIdentity Operation);

/// <summary>
/// Consume outcome. A repeated (deduplicated) consume reports
/// <see cref="AlreadyConsumed"/> with the remaining capacity as of the replay.
/// </summary>
public sealed record ConsumeCapacityResult(bool AlreadyConsumed, decimal? Remaining);

/// <summary>
/// Producer-owned release/compensation request. Releasing restores capacity and
/// records a negative ledger delta under a distinct LogicalOperationId.
/// </summary>
public sealed record ReleaseCapacityRequest(BillingCapacityOperationIdentity Operation);

/// <summary>
/// Release outcome. A repeated (deduplicated) release reports
/// <see cref="AlreadyReleased"/> with the remaining capacity as of the replay.
/// </summary>
public sealed record ReleaseCapacityResult(bool AlreadyReleased, decimal? Remaining);

/// <summary>
/// Deterministic failure for reusing a logical operation id with a different
/// payload. The first execution wins; the conflicting retry never mutates
/// Billing usage state.
/// </summary>
public sealed class CapacityOperationConflictException(Guid logicalOperationId)
    : Exception($"Capacity operation '{logicalOperationId}' was already recorded with a different payload.");

/// <summary>
/// Producer-owned public capacity surface for Billing-managed hard quotas.
/// Callers request consume/release of a capability slot; Billing decides the
/// capacity invariant against the authoritative WorkspaceFeatureUsage record
/// and records every effect in the usage ledger. Exceptions:
/// <list type="bullet">
/// <item>over-capacity consume (semantic business-rule violation)</item>
/// <item>release below zero (semantic business-rule violation)</item>
/// <item>conflicting retry of an executed LogicalOperationId (deterministic conflict)</item>
/// <item>concurrent last-slot contention (concurrency conflict, exactly one winner)</item>
/// </list>
/// </summary>
public interface IBillingCapacityActions
{
    Task<ConsumeCapacityResult> ConsumeAsync(
        ConsumeCapacityRequest request,
        CancellationToken cancellationToken);

    Task<ReleaseCapacityResult> ReleaseAsync(
        ReleaseCapacityRequest request,
        CancellationToken cancellationToken);
}