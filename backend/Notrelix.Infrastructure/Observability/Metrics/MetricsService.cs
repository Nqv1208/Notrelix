namespace Notrelix.Infrastructure.Observability.Metrics;

/// <summary>
/// Skeleton metrics service (v4 §18). Real implementation emits the required
/// metrics: outbox.pending/failed/dispatch.latency, worker.heartbeat.age,
/// cache.hit_rate, permission.evaluate.duration, db.query.duration,
/// webhook.delivery.failed. Not yet wired.
/// </summary>
public sealed class MetricsService
{
    // TODO(v4 §18): wrap System.Diagnostics.Metrics.Meter / counters + histograms.
}
