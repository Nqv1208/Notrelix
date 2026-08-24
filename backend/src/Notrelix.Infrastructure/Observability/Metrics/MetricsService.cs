using System.Diagnostics.Metrics;

namespace Notrelix.Infrastructure.Observability.Metrics;

public sealed class MetricsService : IDisposable
{
    public const string MeterName = "Notrelix.Infrastructure";

    private readonly Meter _meter;
    private int _currentPending;
    private int _currentFailed;
    private int _currentDeadLetter;
    private double _currentOldestAgeMs;

    public MetricsService()
    {
        _meter = new Meter(MeterName);

        _meter.CreateObservableGauge<int>("outbox_pending_count", () => _currentPending,
            description: "Number of pending outbox messages");
        _meter.CreateObservableGauge<int>("outbox_failed_count", () => _currentFailed,
            description: "Number of failed outbox messages");
        _meter.CreateObservableGauge<int>("outbox_dead_letter_count", () => _currentDeadLetter,
            description: "Number of dead-letter outbox messages");
        _meter.CreateObservableGauge<double>("outbox_oldest_age_ms", () => _currentOldestAgeMs,
            description: "Age of the oldest undispatched outbox message in milliseconds");

        OutboxDispatchedCount = _meter.CreateCounter<long>("outbox_dispatched_count",
            description: "Count of successfully dispatched outbox messages");
        PublishFailures = _meter.CreateCounter<long>("publish_failures",
            description: "Count of failed outbox message dispatches");
        CommitToClaim = _meter.CreateHistogram<double>("commit_to_claim_ms",
            unit: "ms", description: "Lag between message commit and dispatcher claim in milliseconds");
        CommitToPublish = _meter.CreateHistogram<double>("commit_to_publish_ms",
            unit: "ms", description: "Lag between message commit and successful broker publish in milliseconds");
        OutboxPublishDuration = _meter.CreateHistogram<double>("outbox_publish_duration_ms",
            unit: "ms", description: "Duration of a single outbox broker publish in milliseconds");
        RealtimePublishDuration = _meter.CreateHistogram<double>("realtime_publish_ms",
            unit: "ms", description: "Duration of realtime publisher fan-out in milliseconds");
        InboxDuplicates = _meter.CreateCounter<long>("inbox_duplicates",
            description: "Count of inbox claim conflicts caused by duplicate event delivery");
    }

    public Counter<long> OutboxDispatchedCount { get; }
    public Counter<long> PublishFailures { get; }
    public Counter<long> InboxDuplicates { get; }
    public Histogram<double> CommitToClaim { get; }
    public Histogram<double> CommitToPublish { get; }
    public Histogram<double> OutboxPublishDuration { get; }
    public Histogram<double> RealtimePublishDuration { get; }

    public void UpdateOutboxCounts(int pending, int failed, int deadLetter, double? oldestAgeMs)
    {
        Interlocked.Exchange(ref _currentPending, pending);
        Interlocked.Exchange(ref _currentFailed, failed);
        Interlocked.Exchange(ref _currentDeadLetter, deadLetter);
        Interlocked.Exchange(ref _currentOldestAgeMs, oldestAgeMs ?? 0d);
    }

    public void Dispose() => _meter.Dispose();
}
