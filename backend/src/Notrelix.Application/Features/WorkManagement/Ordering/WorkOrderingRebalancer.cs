using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>A sibling's rebalanced position.</summary>
public sealed record RebalancedPosition(Guid Id, FractionalIndex Position);

/// <summary>Statistics of one rebalance run, recorded for ordering-health monitoring.</summary>
public sealed record WorkOrderingRebalanceResult(int SiblingCount, int MaxKeyLengthBefore, int MaxKeyLengthAfter);

/// <summary>
/// Explicit maintenance rebalance of one ordering scope (WM-V3-ORDER-004,
/// WMREQ053–054). It is never the normal drag path: it takes the same scope lock,
/// reads the siblings in canonical order (ordinal position, then stable id), and
/// rewrites their keys with <see cref="FractionalIndexGenerator.GenerateNKeysBetween"/>
/// so the logical order is exactly preserved while key length shrinks.
/// </summary>
public sealed class WorkOrderingRebalancer
{
    private readonly IWorkOrderingScopeLock _scopeLock;

    public WorkOrderingRebalancer(IWorkOrderingScopeLock scopeLock)
    {
        _scopeLock = scopeLock;
    }

    /// <param name="loadActiveSiblings">Invoked only after the scope lock is held.</param>
    /// <param name="applyPositions">Persists the new keys; receives them in the preserved logical order.</param>
    public async Task<WorkOrderingRebalanceResult> RebalanceAsync(
        WorkOrderingScope scope,
        Func<CancellationToken, Task<IReadOnlyList<OrderedSibling>>> loadActiveSiblings,
        Func<IReadOnlyList<RebalancedPosition>, CancellationToken, Task> applyPositions,
        CancellationToken cancellationToken)
    {
        await _scopeLock.AcquireAsync(scope, cancellationToken);

        var siblings = (await loadActiveSiblings(cancellationToken))
            .OrderBy(sibling => sibling.Position)
            .ThenBy(sibling => sibling.Id)
            .ToList();

        if (siblings.Count == 0)
        {
            return new WorkOrderingRebalanceResult(0, 0, 0);
        }

        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, siblings.Count);
        var positions = siblings.Select((sibling, index) => new RebalancedPosition(sibling.Id, keys[index])).ToList();

        await applyPositions(positions, cancellationToken);

        return new WorkOrderingRebalanceResult(
            siblings.Count,
            siblings.Max(sibling => sibling.Position.Value.Length),
            positions.Max(position => position.Position.Value.Length));
    }
}
