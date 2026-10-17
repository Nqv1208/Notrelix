using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Ordering;

/// <summary>
/// ORDER-PLACE-001 / ORDER-STALE-001 (WM-V3-ORDER-002). Neighbor-relative placement:
/// both absent appends, previous-only must be the current last sibling, next-only
/// the current first, both must be currently adjacent; anything else is a stale
/// placement and generates no key. The scope lock is held before siblings are read.
/// </summary>
public sealed class WorkPlacementResolverTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private static readonly IReadOnlyList<OrderedSibling> ThreeSiblings =
    [
        new(A, FractionalIndex.Create("a0")),
        new(B, FractionalIndex.Create("a1")),
        new(C, FractionalIndex.Create("a2")),
    ];

    [Fact]
    public void BothAbsent_OnEmptyScope_ReturnsAnInitialKey()
    {
        var key = WorkPlacementResolver.Place([], WorkPlacementIntent.Append);

        key.Should().NotBeNull();
    }

    [Fact]
    public void BothAbsent_AppendsAfterTheCurrentLast()
    {
        var key = WorkPlacementResolver.Place(ThreeSiblings, WorkPlacementIntent.Append);

        key.CompareTo(ThreeSiblings[2].Position).Should().BePositive();
    }

    [Fact]
    public void PreviousOnly_WhenPreviousIsLast_AppendsAfterIt()
    {
        var key = WorkPlacementResolver.Place(ThreeSiblings, new WorkPlacementIntent(C, null));

        key.CompareTo(ThreeSiblings[2].Position).Should().BePositive();
    }

    [Fact]
    public void NextOnly_WhenNextIsFirst_PrependsBeforeIt()
    {
        var key = WorkPlacementResolver.Place(ThreeSiblings, new WorkPlacementIntent(null, A));

        key.CompareTo(ThreeSiblings[0].Position).Should().BeNegative();
    }

    [Fact]
    public void Both_WhenAdjacent_PlacesStrictlyBetween()
    {
        var key = WorkPlacementResolver.Place(ThreeSiblings, new WorkPlacementIntent(A, B));

        key.CompareTo(ThreeSiblings[0].Position).Should().BePositive();
        key.CompareTo(ThreeSiblings[1].Position).Should().BeNegative();
    }

    [Theory]
    [MemberData(nameof(StaleIntents))]
    public void StaleIntent_IsRejectedWithTheStablePlacementCode(Guid? previous, Guid? next)
    {
        var act = () => WorkPlacementResolver.Place(ThreeSiblings, new WorkPlacementIntent(previous, next));

        act.Should().Throw<ConflictException>().Which.ErrorCode.Should().Be(WorkOrderingErrorCodes.StalePlacement);
    }

    public static TheoryData<Guid?, Guid?> StaleIntents => new()
    {
        { B, null },               // previous-only but not the last sibling
        { null, B },               // next-only but not the first sibling
        { A, C },                  // both present but not adjacent
        { B, A },                  // adjacent in the wrong order
        { A, A },                  // same sibling on both sides
        { Guid.NewGuid(), null },  // unknown previous sibling
    };

    [Fact]
    public void EqualLegacyKeys_LeaveNoGap_AndAreRejectedAsStale()
    {
        IReadOnlyList<OrderedSibling> duplicates = [new(A, FractionalIndex.Create("a0")), new(B, FractionalIndex.Create("a0"))];

        var act = () => WorkPlacementResolver.Place(duplicates, new WorkPlacementIntent(A, B));

        act.Should().Throw<ConflictException>().Which.ErrorCode.Should().Be(WorkOrderingErrorCodes.StalePlacement);
    }

    [Fact]
    public async Task Resolve_AcquiresTheScopeLockBeforeReadingSiblings()
    {
        var events = new List<string>();
        var resolver = new WorkPlacementResolver(new RecordingLock(events));
        var scope = new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid());

        await resolver.ResolveAsync(scope, WorkPlacementIntent.Append, _ =>
        {
            events.Add("load");
            return Task.FromResult(ThreeSiblings);
        }, CancellationToken.None);

        events.Should().Equal($"lock:{scope.KindName}:{scope.ParentId}", "load");
    }

    [Fact]
    public async Task Resolve_ForAMove_ExcludesTheMovedEntityBeforeAdjacency()
    {
        var resolver = new WorkPlacementResolver(new RecordingLock([]));

        // Moving B to the end: once B is excluded, A and C are adjacent and C is last.
        var key = await resolver.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid()),
            new WorkPlacementIntent(C, null),
            _ => Task.FromResult(ThreeSiblings),
            CancellationToken.None,
            movedEntityId: B);

        key.CompareTo(ThreeSiblings[2].Position).Should().BePositive();
    }

    [Fact]
    public async Task Resolve_OrdersSiblingsOrdinally_RegardlessOfLoadOrder()
    {
        var resolver = new WorkPlacementResolver(new RecordingLock([]));
        IReadOnlyList<OrderedSibling> unordered = [ThreeSiblings[2], ThreeSiblings[0], ThreeSiblings[1]];

        var key = await resolver.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid()),
            new WorkPlacementIntent(A, B),
            _ => Task.FromResult(unordered),
            CancellationToken.None);

        key.CompareTo(ThreeSiblings[0].Position).Should().BePositive();
        key.CompareTo(ThreeSiblings[1].Position).Should().BeNegative();
    }

    private sealed class RecordingLock(List<string> events) : IWorkOrderingScopeLock
    {
        public Task AcquireAsync(WorkOrderingScope scope, CancellationToken cancellationToken)
        {
            events.Add($"lock:{scope.KindName}:{scope.ParentId}");
            return Task.CompletedTask;
        }
    }
}
