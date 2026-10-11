using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Neighbor-relative sibling placement of a Space (same contract as Work
/// ordering, WM-V3-DEC-002): both neighbors absent appends; previous-only places
/// after the current last sibling; next-only before the current first sibling;
/// both between two currently adjacent siblings. A neighbor intent that no
/// longer matches the current order is rejected as stale (409), never guessed.
/// Must run on siblings read under the hierarchy lock.
/// </summary>
public static class SpacePlacementResolver
{
    public const string StalePlacementErrorCode = "workspaces.space.stale-placement";

    public static FractionalIndex Place(
        IReadOnlyList<SpaceSiblingEntry> siblings,
        Guid? previousSiblingId,
        Guid? nextSiblingId,
        Guid? movedSpaceId = null)
    {
        var ordered = siblings
            .OrderBy(s => s.Position)
            .ThenBy(s => s.Id)
            .ToList();

        if (movedSpaceId is { } movedId && TryKeepCurrent(ordered, movedId, previousSiblingId, nextSiblingId, out var current))
            return current;

        ordered = ordered.Where(s => s.Id != movedSpaceId).ToList();

        if (previousSiblingId is null && nextSiblingId is null)
            return FractionalIndexGenerator.GenerateKeyBetween(ordered.LastOrDefault()?.Position, null);

        if (previousSiblingId is { } prevOnly && nextSiblingId is null)
        {
            var last = ordered.LastOrDefault();
            if (last is null || last.Id != prevOnly) throw Stale();
            return FractionalIndexGenerator.GenerateKeyBetween(last.Position, null);
        }

        if (previousSiblingId is null && nextSiblingId is { } nextOnly)
        {
            var first = ordered.FirstOrDefault();
            if (first is null || first.Id != nextOnly) throw Stale();
            return FractionalIndexGenerator.GenerateKeyBetween(null, first.Position);
        }

        if (previousSiblingId == nextSiblingId) throw Stale();

        var previousIndex = ordered.FindIndex(s => s.Id == previousSiblingId);
        if (previousIndex < 0 || previousIndex + 1 >= ordered.Count || ordered[previousIndex + 1].Id != nextSiblingId)
            throw Stale();

        var previous = ordered[previousIndex].Position;
        var next = ordered[previousIndex + 1].Position;
        if (previous.CompareTo(next) >= 0) throw Stale();

        return FractionalIndexGenerator.GenerateKeyBetween(previous, next);
    }

    private static bool TryKeepCurrent(
        List<SpaceSiblingEntry> ordered, Guid movedId, Guid? previousId, Guid? nextId, out FractionalIndex current)
    {
        current = null!;
        var index = ordered.FindIndex(s => s.Id == movedId);
        if (index < 0) return false;

        var before = index > 0 ? ordered[index - 1].Id : (Guid?)null;
        var after = index + 1 < ordered.Count ? ordered[index + 1].Id : (Guid?)null;
        var matches = (previousId, nextId) switch
        {
            (null, null) => after is null,
            ({ } previous, null) => before == previous && after is null,
            (null, { } next) => after == next && before is null,
            ({ } previous, { } next) => before == previous && after == next,
        };

        if (matches) current = ordered[index].Position;
        return matches;
    }

    private static ConflictException Stale() =>
        new("The sibling placement no longer matches the current space order. Refresh and retry.", StalePlacementErrorCode);
}
