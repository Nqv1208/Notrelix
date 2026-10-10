using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// Neighbor-relative placement intent (SPEC WM-V3-DEC-002). Both absent means
/// append; previous-only means after the current last sibling; next-only means
/// before the current first sibling; both means between currently adjacent siblings.
/// </summary>
public sealed record WorkPlacementIntent(Guid? PreviousSiblingId, Guid? NextSiblingId)
{
    public static WorkPlacementIntent Append { get; } = new(null, null);
}

/// <summary>An active sibling of an ordering scope with its persisted position.</summary>
public sealed record OrderedSibling(Guid Id, FractionalIndex Position);

/// <summary>
/// The canonical placement rule for every normal P3 ordering mutation
/// (WM-V3-ORDER-002). It acquires the scope lock before the siblings are read,
/// validates the declared neighbors against the current sibling order, and
/// generates exactly one key with <see cref="FractionalIndexGenerator"/>.
/// </summary>
public sealed class WorkPlacementResolver
{
    private readonly IWorkOrderingScopeLock _scopeLock;

    public WorkPlacementResolver(IWorkOrderingScopeLock scopeLock)
    {
        _scopeLock = scopeLock;
    }

    /// <param name="loadActiveSiblings">
    /// Loads the scope's active siblings. It is invoked only after the scope lock is
    /// held, so callers must not read siblings before calling this method.
    /// </param>
    /// <param name="movedEntityId">For a move, the entity being moved; it is excluded before adjacency is evaluated.</param>
    public async Task<FractionalIndex> ResolveAsync(
        WorkOrderingScope scope,
        WorkPlacementIntent intent,
        Func<CancellationToken, Task<IReadOnlyList<OrderedSibling>>> loadActiveSiblings,
        CancellationToken cancellationToken,
        Guid? movedEntityId = null)
    {
        await _scopeLock.AcquireAsync(scope, cancellationToken);

        var siblings = (await loadActiveSiblings(cancellationToken))
            .Where(sibling => sibling.Id != movedEntityId)
            .OrderBy(sibling => sibling.Position)
            .ThenBy(sibling => sibling.Id)
            .ToList();

        return Place(siblings, intent);
    }

    /// <summary>Pure placement rule over siblings already sorted in canonical order.</summary>
    public static FractionalIndex Place(IReadOnlyList<OrderedSibling> orderedSiblings, WorkPlacementIntent intent)
    {
        var (previousId, nextId) = (intent.PreviousSiblingId, intent.NextSiblingId);

        if (previousId is null && nextId is null)
        {
            return FractionalIndexGenerator.GenerateKeyBetween(orderedSiblings.LastOrDefault()?.Position, null);
        }

        if (previousId is not null && nextId is null)
        {
            var last = orderedSiblings.LastOrDefault();
            if (last is null || last.Id != previousId) throw StalePlacement();
            return FractionalIndexGenerator.GenerateKeyBetween(last.Position, null);
        }

        if (previousId is null && nextId is not null)
        {
            var first = orderedSiblings.FirstOrDefault();
            if (first is null || first.Id != nextId) throw StalePlacement();
            return FractionalIndexGenerator.GenerateKeyBetween(null, first.Position);
        }

        if (previousId == nextId) throw StalePlacement();

        var previousIndex = IndexOf(orderedSiblings, previousId!.Value);
        if (previousIndex < 0 || previousIndex + 1 >= orderedSiblings.Count || orderedSiblings[previousIndex + 1].Id != nextId)
        {
            throw StalePlacement();
        }

        var previous = orderedSiblings[previousIndex].Position;
        var next = orderedSiblings[previousIndex + 1].Position;

        // Equal legacy keys leave no gap to place into; the scope must be
        // rebalanced or the client must rebase.
        if (previous.CompareTo(next) >= 0) throw StalePlacement();

        return FractionalIndexGenerator.GenerateKeyBetween(previous, next);
    }

    private static int IndexOf(IReadOnlyList<OrderedSibling> siblings, Guid id)
    {
        for (var i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].Id == id) return i;
        }

        return -1;
    }

    private static ConflictException StalePlacement() =>
        new("The declared neighbors are no longer adjacent. Reload and retry.", WorkOrderingErrorCodes.StalePlacement);
}
