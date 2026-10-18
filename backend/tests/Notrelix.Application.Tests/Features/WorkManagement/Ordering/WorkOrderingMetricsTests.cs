using System.Diagnostics.Metrics;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Ordering;

/// <summary>
/// ORDER-HEALTH-001 (WM-V3-QUAL-002). The ordering mechanism emits key length,
/// stale-placement and rebalance telemetry, tagged by scope kind only.
/// </summary>
public sealed class WorkOrderingMetricsTests : IDisposable
{
    private static readonly WorkOrderingScope Scope = new(WorkOrderingScopeKind.GroupItems, Guid.NewGuid());
    private readonly WorkOrderingMetrics _metrics = new();
    private readonly MeterListener _listener = new();
    private readonly List<(string Instrument, long Value, string? ScopeTag)> _measurements = [];

    public WorkOrderingMetricsTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == WorkOrderingMetrics.MeterName) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((i, v, tags, _) => Record(i.Name, v, tags));
        _listener.SetMeasurementEventCallback<long>((i, v, tags, _) => Record(i.Name, v, tags));
        _listener.Start();
    }

    private void Record(string name, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        string? scope = null;
        foreach (var tag in tags) if (tag.Key == "ordering.scope") scope = tag.Value as string;
        lock (_measurements) _measurements.Add((name, value, scope));
    }

    private static readonly IWorkOrderingScopeLock NoLock = Mock.Of<IWorkOrderingScopeLock>();

    [Fact]
    public async Task ResolvedPlacement_RecordsTheKeyLength_ForItsScope()
    {
        var resolver = new WorkPlacementResolver(NoLock, _metrics);

        var key = await resolver.ResolveAsync(Scope, WorkPlacementIntent.Append, _ => Task.FromResult<IReadOnlyList<OrderedSibling>>([]), CancellationToken.None);

        _measurements.Should().Contain(("work_ordering_key_length", key.Value.Length, "group-items"));
    }

    [Fact]
    public async Task StalePlacement_IsCounted_AndStillRejected()
    {
        var resolver = new WorkPlacementResolver(NoLock, _metrics);

        var act = () => resolver.ResolveAsync(Scope, new WorkPlacementIntent(Guid.NewGuid(), null),
            _ => Task.FromResult<IReadOnlyList<OrderedSibling>>([]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _measurements.Should().Contain(("work_ordering_stale_placement", 1, "group-items"));
    }

    [Fact]
    public async Task Rebalance_IsCountedOncePerScopeRun()
    {
        var rebalancer = new WorkOrderingRebalancer(NoLock, _metrics);
        IReadOnlyList<OrderedSibling> siblings = [new(Guid.NewGuid(), FractionalIndex.Create("a0")), new(Guid.NewGuid(), FractionalIndex.Create("a0V"))];

        await rebalancer.RebalanceAsync(Scope, _ => Task.FromResult(siblings), (_, _) => Task.CompletedTask, CancellationToken.None);

        _measurements.Should().ContainSingle(m => m.Instrument == "work_ordering_rebalance_runs").Which.ScopeTag.Should().Be("group-items");
    }

    public void Dispose()
    {
        _listener.Dispose();
        _metrics.Dispose();
    }
}
