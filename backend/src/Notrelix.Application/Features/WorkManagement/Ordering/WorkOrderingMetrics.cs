using System.Diagnostics.Metrics;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// Ordering-health telemetry of the P3 ordering scopes (WM-V3-QUAL-002, SPEC
/// WMREQ148–150): resolved key length (the signal that a scope needs a
/// maintenance rebalance), stale-placement rejections, unique-index ordering
/// conflicts and rebalance runs. Tagged by scope kind only, never by tenant or
/// entity id.
/// </summary>
public sealed class WorkOrderingMetrics : IDisposable
{
    public const string MeterName = "Notrelix.WorkManagement.Ordering";

    private readonly Meter _meter;
    private readonly Histogram<int> _keyLength;
    private readonly Counter<long> _stalePlacements;
    private readonly Counter<long> _conflicts;
    private readonly Counter<long> _rebalanceRuns;

    public WorkOrderingMetrics()
    {
        _meter = new Meter(MeterName);
        _keyLength = _meter.CreateHistogram<int>("work_ordering_key_length", unit: "{character}",
            description: "Length of each ordering key resolved for a placement.");
        _stalePlacements = _meter.CreateCounter<long>("work_ordering_stale_placement", unit: "{rejection}",
            description: "Placements rejected because the declared neighbors were no longer adjacent.");
        _conflicts = _meter.CreateCounter<long>("work_ordering_conflict", unit: "{conflict}",
            description: "Writes rejected by a named unique ordering index.");
        _rebalanceRuns = _meter.CreateCounter<long>("work_ordering_rebalance_runs", unit: "{run}",
            description: "Maintenance rebalances of one ordering scope.");
    }

    public void KeyResolved(WorkOrderingScope scope, FractionalIndex key) =>
        _keyLength.Record(key.Value.Length, new KeyValuePair<string, object?>("ordering.scope", scope.KindName));

    public void StalePlacement(WorkOrderingScope scope) =>
        _stalePlacements.Add(1, new KeyValuePair<string, object?>("ordering.scope", scope.KindName));

    public void Conflict() => _conflicts.Add(1);

    public void Rebalanced(WorkOrderingScope scope) =>
        _rebalanceRuns.Add(1, new KeyValuePair<string, object?>("ordering.scope", scope.KindName));

    public void Dispose() => _meter.Dispose();
}
