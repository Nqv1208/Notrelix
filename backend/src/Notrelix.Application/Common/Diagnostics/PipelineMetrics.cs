using System.Diagnostics.Metrics;

namespace Notrelix.Application.Common.Diagnostics;

/// <summary>
/// Canonical application-pipeline meter. Low-cardinality instruments only:
/// labels are code-bounded categories (stage/request kind/error category) —
/// never user/tenant/resource ids or raw exception text.
/// </summary>
public sealed class PipelineMetrics : IDisposable
{
    public const string MeterName = "Notrelix.Application.Pipeline";

    private readonly Meter _meter;

    public PipelineMetrics()
    {
        _meter = new Meter(MeterName);
        ExpectedVersionConflicts = _meter.CreateCounter<long>(
            "pipeline_expected_version_conflict",
            unit: "{conflict}",
            description: "Optimistic concurrency precondition failures inside the data session.");
        ExpectedVersionBindingMisconfigurations = _meter.CreateCounter<long>(
            "pipeline_expected_version_binding_misconfiguration",
            unit: "{failure}",
            description: "Declared expected-version constraints that could not be bound to a tracked aggregate.");
    }

    public Counter<long> ExpectedVersionConflicts { get; }

    public Counter<long> ExpectedVersionBindingMisconfigurations { get; }

    public void Dispose() => _meter.Dispose();
}
