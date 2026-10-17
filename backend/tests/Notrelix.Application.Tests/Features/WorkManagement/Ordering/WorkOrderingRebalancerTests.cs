using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Ordering;

/// <summary>
/// ORDER-REBAL-001 (WM-V3-ORDER-004). Rebalance holds the scope lock before reading,
/// preserves the exact logical order (ordinal position, then stable id for equal
/// legacy keys) and produces strictly increasing canonical keys.
/// </summary>
public sealed class WorkOrderingRebalancerTests
{
    [Fact]
    public async Task Rebalance_PreservesLogicalOrder_AndProducesIncreasingKeys()
    {
        var a = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var b = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var c = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        IReadOnlyList<OrderedSibling> siblings =
        [
            new(c, FractionalIndex.Create("a1zzzzzzzzzzzzz")),
            new(b, FractionalIndex.Create("a0")),
            new(a, FractionalIndex.Create("a0")),
        ];
        IReadOnlyList<RebalancedPosition>? applied = null;
        var events = new List<string>();

        var result = await new WorkOrderingRebalancer(new RecordingLock(events)).RebalanceAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, Guid.NewGuid()),
            _ => { events.Add("load"); return Task.FromResult(siblings); },
            (positions, _) => { applied = positions; return Task.CompletedTask; },
            CancellationToken.None);

        events.Should().Equal("lock", "load");
        applied!.Select(p => p.Id).Should().Equal([a, b, c], "equal legacy keys keep their stable-id tie-break order");
        applied.Select(p => p.Position).Should().BeInAscendingOrder();
        applied.Select(p => p.Position.Value).Should().OnlyHaveUniqueItems();
        result.Should().Be(new WorkOrderingRebalanceResult(3, 15, applied.Max(p => p.Position.Value.Length)));
    }

    [Fact]
    public async Task Rebalance_OfAnEmptyScope_WritesNothing()
    {
        var wrote = false;

        var result = await new WorkOrderingRebalancer(new RecordingLock([])).RebalanceAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, Guid.NewGuid()),
            _ => Task.FromResult<IReadOnlyList<OrderedSibling>>([]),
            (_, _) => { wrote = true; return Task.CompletedTask; },
            CancellationToken.None);

        wrote.Should().BeFalse();
        result.SiblingCount.Should().Be(0);
    }

    private sealed class RecordingLock(List<string> events) : IWorkOrderingScopeLock
    {
        public Task AcquireAsync(WorkOrderingScope scope, CancellationToken cancellationToken)
        {
            events.Add("lock");
            return Task.CompletedTask;
        }
    }
}
