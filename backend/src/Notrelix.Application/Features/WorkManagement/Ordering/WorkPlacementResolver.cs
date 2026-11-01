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

/// <summary>
/// An active sibling of an ordering scope with its persisted position. A hidden
/// sibling (archived) is not on any client's screen but still holds its key in the
/// unique ordering index: neighbors are validated against the visible siblings and
/// the new key is generated against all of them.
/// </summary>
public sealed record OrderedSibling(Guid Id, FractionalIndex Position, bool Hidden)
{
    public OrderedSibling(Guid id, FractionalIndex position) : this(id, position, false)
    {
    }
}

/// <summary>
/// The canonical placement rule for every normal P3 ordering mutation
/// (WM-V3-ORDER-002). It acquires the scope lock before the siblings are read,
/// validates the declared neighbors against the current sibling order, and
/// generates exactly one key with <see cref="FractionalIndexGenerator"/>.
/// </summary>
public sealed class WorkPlacementResolver
{
    private readonly IWorkOrderingScopeLock _scopeLock;

    private readonly WorkOrderingMetrics? _metrics;

    public WorkPlacementResolver(IWorkOrderingScopeLock scopeLock, WorkOrderingMetrics? metrics = null)
    {
        _scopeLock = scopeLock;
        _metrics = metrics;
    }

    /// <summary>Records the resolved key length, or a stale-placement rejection.</summary>
    private FractionalIndex Observe(WorkOrderingScope scope, Func<FractionalIndex> place)
    {
        try
        {
            var key = place();
            _metrics?.KeyResolved(scope, key);
            return key;
        }
        catch (ConflictException ex) when (ex.ErrorCode == WorkOrderingErrorCodes.StalePlacement)
        {
            _metrics?.StalePlacement(scope);
            throw;
        }
    }

    /// <param name="loadActiveSiblings">
    /// Loads the scope's active siblings. It is invoked only after the scope lock is
    /// held, so callers must not read siblings before calling this method.
    /// </param>
    /// <param name="movedEntityId">
    /// For a move, the entity being moved; it is excluded before adjacency is evaluated.
    /// When it is already in this scope exactly where the intent places it, its current
    /// position is returned so the move is a semantic no-op.
    /// </param>
    public async Task<FractionalIndex> ResolveAsync(
        WorkOrderingScope scope,
        WorkPlacementIntent intent,
        Func<CancellationToken, Task<IReadOnlyList<OrderedSibling>>> loadActiveSiblings,
        CancellationToken cancellationToken,
        Guid? movedEntityId = null)
    {
        await _scopeLock.AcquireAsync(scope, cancellationToken);

        var ordered = (await loadActiveSiblings(cancellationToken))
            .OrderBy(sibling => sibling.Position)
            .ThenBy(sibling => sibling.Id)
            .ToList();

        if (movedEntityId is { } movedId && TryKeepCurrent(ordered, movedId, intent, out var current))
        {
            return current;
        }

        return Observe(scope, () => Place(ordered.Where(sibling => sibling.Id != movedEntityId).ToList(), intent));
    }

    /// <summary>
    /// Places a new entity immediately after <paramref name="anchorId"/> (between the
    /// anchor and its current successor), under the scope lock. Used by duplicate.
    /// </summary>
    public async Task<FractionalIndex> ResolveAfterAsync(
        WorkOrderingScope scope,
        Guid anchorId,
        Func<CancellationToken, Task<IReadOnlyList<OrderedSibling>>> loadActiveSiblings,
        CancellationToken cancellationToken)
    {
        await _scopeLock.AcquireAsync(scope, cancellationToken);

        var ordered = (await loadActiveSiblings(cancellationToken))
            .OrderBy(sibling => sibling.Position)
            .ThenBy(sibling => sibling.Id)
            .ToList();

        return Observe(scope, () =>
        {
            var index = IndexOf(ordered, anchorId);
            if (index < 0) throw StalePlacement();
            return KeyAfter(ordered, index);
        });
    }

    /// <summary>
    /// Restore placement (WM-V3-DATA-002): returns <c>null</c> when the restored
    /// entity's previous key is still free in the scope, otherwise a key directly
    /// after the active sibling that now holds it, so restore never collides with
    /// the unique ordering index. Runs under the scope lock.
    /// </summary>
    public async Task<FractionalIndex?> ResolveRestoreAsync(
        WorkOrderingScope scope,
        FractionalIndex previousPosition,
        Func<CancellationToken, Task<IReadOnlyList<OrderedSibling>>> loadActiveSiblings,
        CancellationToken cancellationToken)
    {
        await _scopeLock.AcquireAsync(scope, cancellationToken);

        var ordered = (await loadActiveSiblings(cancellationToken))
            .OrderBy(sibling => sibling.Position)
            .ThenBy(sibling => sibling.Id)
            .ToList();

        var holder = ordered.FindIndex(sibling => sibling.Position.Value == previousPosition.Value);
        if (holder < 0) return null;

        return Observe(scope, () => KeyAfter(ordered, holder));
    }

    private static bool TryKeepCurrent(
        IReadOnlyList<OrderedSibling> ordered, Guid movedId, WorkPlacementIntent intent, out FractionalIndex current)
    {
        current = null!;
        var visible = Visible(ordered);
        var index = IndexOf(visible, movedId);
        if (index < 0) return false;

        var before = index > 0 ? visible[index - 1].Id : (Guid?)null;
        var after = index + 1 < visible.Count ? visible[index + 1].Id : (Guid?)null;
        var matches = (intent.PreviousSiblingId, intent.NextSiblingId) switch
        {
            (null, null) => after is null,
            ({ } previous, null) => before == previous && after is null,
            (null, { } next) => after == next && before is null,
            ({ } previous, { } next) => before == previous && after == next,
        };

        if (matches) current = visible[index].Position;
        return matches;
    }

    /// <summary>A key directly after the sibling at <paramref name="index"/>, in front of whatever follows it (hidden siblings included).</summary>
    private static FractionalIndex KeyAfter(IReadOnlyList<OrderedSibling> ordered, int index)
    {
        var previous = ordered[index].Position;
        FractionalIndex? next = index + 1 < ordered.Count ? ordered[index + 1].Position : null;
        if (next is not null && previous.CompareTo(next) >= 0) throw StalePlacement();
        return FractionalIndexGenerator.GenerateKeyBetween(previous, next);
    }

    private static IReadOnlyList<OrderedSibling> Visible(IReadOnlyList<OrderedSibling> siblings) =>
        siblings.Any(sibling => sibling.Hidden) ? siblings.Where(sibling => !sibling.Hidden).ToList() : siblings;

    /// <summary>Pure placement rule over siblings already sorted in canonical order.</summary>
    public static FractionalIndex Place(IReadOnlyList<OrderedSibling> orderedSiblings, WorkPlacementIntent intent)
    {
        var (previousId, nextId) = (intent.PreviousSiblingId, intent.NextSiblingId);
        var visible = Visible(orderedSiblings);

        if (previousId is null && nextId is null)
        {
            return FractionalIndexGenerator.GenerateKeyBetween(orderedSiblings.LastOrDefault()?.Position, null);
        }

        if (previousId is not null && nextId is null)
        {
            var last = visible.LastOrDefault();
            if (last is null || last.Id != previousId) throw StalePlacement();
            return FractionalIndexGenerator.GenerateKeyBetween(orderedSiblings[^1].Position, null);
        }

        if (previousId is null && nextId is not null)
        {
            var first = visible.FirstOrDefault();
            if (first is null || first.Id != nextId) throw StalePlacement();
            return FractionalIndexGenerator.GenerateKeyBetween(null, orderedSiblings[0].Position);
        }

        if (previousId == nextId) throw StalePlacement();

        var previousIndex = IndexOf(visible, previousId!.Value);
        if (previousIndex < 0 || previousIndex + 1 >= visible.Count || visible[previousIndex + 1].Id != nextId)
        {
            throw StalePlacement();
        }

        var previous = visible[previousIndex].Position;
        var next = visible[previousIndex + 1].Position;

        // Equal legacy keys leave no gap to place into; the scope must be
        // rebalanced or the client must rebase.
        if (previous.CompareTo(next) >= 0) throw StalePlacement();

        // A hidden sibling between the two neighbors keeps its key: the new key goes
        // directly after the previous neighbor, in front of whatever follows it.
        var upper = orderedSiblings[IndexOf(orderedSiblings, previousId.Value) + 1].Position;
        return FractionalIndexGenerator.GenerateKeyBetween(previous, upper);
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
