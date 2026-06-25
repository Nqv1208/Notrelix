using System.Diagnostics.Metrics;
using System.Threading;

namespace Notrelix.Infrastructure.Observability.Metrics;

public sealed class MetricsService
{
    public const string MeterName = "Notrelix.Infrastructure";

    private readonly Meter _meter;
    private int _currentPending;
    private int _currentFailed;
    private int _currentDeadLetter;

    public MetricsService()
    {
        _meter = new Meter(MeterName);

        _meter.CreateObservableGauge<int>("outbox.pending", () => _currentPending,
            description: "Number of pending outbox messages");
        _meter.CreateObservableGauge<int>("outbox.failed", () => _currentFailed,
            description: "Number of failed outbox messages");
        _meter.CreateObservableGauge<int>("outbox.dead_letter", () => _currentDeadLetter,
            description: "Number of dead-letter outbox messages");

        OutboxDispatchedCount = _meter.CreateCounter<long>("outbox.dispatched",
            description: "Count of successfully dispatched outbox messages");
        OutboxFailedDispatchCount = _meter.CreateCounter<long>("outbox.failed_dispatch",
            description: "Count of failed outbox message dispatches");
        OutboxDispatchDuration = _meter.CreateHistogram<double>("outbox.dispatch.duration",
            unit: "ms", description: "Duration of outbox message dispatch in milliseconds");
    }

    public Counter<long> OutboxDispatchedCount { get; }
    public Counter<long> OutboxFailedDispatchCount { get; }
    public Histogram<double> OutboxDispatchDuration { get; }

    public void UpdateOutboxCounts(int pending, int failed, int deadLetter)
    {
        Interlocked.Exchange(ref _currentPending, pending);
        Interlocked.Exchange(ref _currentFailed, failed);
        Interlocked.Exchange(ref _currentDeadLetter, deadLetter);
    }
}
