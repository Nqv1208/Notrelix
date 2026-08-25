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

        N8nDispatchSucceeded = _meter.CreateCounter<long>(
            "n8n_dispatch_succeeded",
            unit: "{dispatch}");
        N8nDispatchFailed = _meter.CreateCounter<long>(
            "n8n_dispatch_failed",
            unit: "{dispatch}");
        N8nDispatchRetries = _meter.CreateCounter<long>(
            "n8n_dispatch_retry",
            unit: "{retry}");
        N8nDispatchDuration = _meter.CreateHistogram<double>(
            "n8n_dispatch_duration",
            unit: "ms");

        Requests = _meter.CreateCounter<long>(
            "pipeline_requests",
            unit: "{request}");
        RequestDuration = _meter.CreateHistogram<double>(
            "pipeline_request_duration",
            unit: "ms");
        StageDuration = _meter.CreateHistogram<double>(
            "pipeline_stage_duration",
            unit: "ms");
        AccessFactsQueryDuration = _meter.CreateHistogram<double>(
            "access_facts_query_duration",
            unit: "ms");
        IdempotencyReplays = _meter.CreateCounter<long>(
            "idempotency_replays",
            unit: "{replay}");
        PipelineFailures = _meter.CreateCounter<long>(
            "pipeline_failures",
            unit: "{failure}");
        HandlerExecutions = _meter.CreateCounter<long>(
            "handler_executions",
            unit: "{execution}");
        DbOperationsPerRequest = _meter.CreateHistogram<long>(
            "db_operations_per_request",
            unit: "{operation}");
    }

    /// <summary>Times a canonical stage and records pipeline_stage_duration{stage}.</summary>
    public StageTimer Stage(string stage) => new(this, stage);

    public readonly struct StageTimer : IDisposable
    {
        private readonly PipelineMetrics _metrics;
        private readonly string _stage;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        public StageTimer(PipelineMetrics metrics, string stage)
        {
            _metrics = metrics;
            _stage = stage;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metrics.StageDuration.Record(
                _stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("stage", _stage));
        }
    }

    public Counter<long> ExpectedVersionConflicts { get; }

    public Counter<long> ExpectedVersionBindingMisconfigurations { get; }

    public Counter<long> N8nDispatchSucceeded { get; }

    public Counter<long> N8nDispatchFailed { get; }

    public Counter<long> N8nDispatchRetries { get; }

    public Histogram<double> N8nDispatchDuration { get; }

    public Counter<long> Requests { get; }

    public Histogram<double> RequestDuration { get; }

    /// <summary>Label 'stage' uses the canonical fixed stage names only.</summary>
    public Histogram<double> StageDuration { get; }

    public Histogram<double> AccessFactsQueryDuration { get; }

    public Counter<long> IdempotencyReplays { get; }

    /// <summary>Label 'error.category' is a fixed code-bounded category.</summary>
    public Counter<long> PipelineFailures { get; }

    public Counter<long> HandlerExecutions { get; }

    public Histogram<long> DbOperationsPerRequest { get; }

    public static readonly KeyValuePair<string, object?>[] NoLabels = [];

    public void Dispose() => _meter.Dispose();
}
