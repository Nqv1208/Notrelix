using System.Diagnostics.Metrics;

namespace Notrelix.Platform.Messaging.Observability;

public sealed class MessagingMetrics : IDisposable
{
    private readonly Meter _meter;

    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _eventsPublishFailed;
    private readonly Counter<long> _eventsDelivered;
    private readonly Counter<long> _eventsDeliveryFailed;
    private readonly Counter<long> _eventsDeadLettered;
    private readonly Counter<long> _circuitBreakerTripped;
    private readonly Counter<long> _circuitBreakerReset;
    private readonly Histogram<double> _publishDuration;
    private readonly Histogram<double> _deliveryDuration;
    private readonly ObservableGauge<long> _activeConnections;

    private long _activeConnectionCount;

    public MessagingMetrics(string meterName = "Notrelix.Platform.Messaging", string version = "1.0.0")
    {
        _meter = new Meter(meterName, version);

        _eventsPublished = _meter.CreateCounter<long>("messaging.events.published");
        _eventsPublishFailed = _meter.CreateCounter<long>("messaging.events.publish_failed");
        _eventsDelivered = _meter.CreateCounter<long>("messaging.events.delivered");
        _eventsDeliveryFailed = _meter.CreateCounter<long>("messaging.events.delivery_failed");
        _eventsDeadLettered = _meter.CreateCounter<long>("messaging.events.dead_lettered");
        _circuitBreakerTripped = _meter.CreateCounter<long>("messaging.circuit_breaker.tripped");
        _circuitBreakerReset = _meter.CreateCounter<long>("messaging.circuit_breaker.reset");
        _publishDuration = _meter.CreateHistogram<double>("messaging.publish.duration", unit: "ms");
        _deliveryDuration = _meter.CreateHistogram<double>("messaging.delivery.duration", unit: "ms");
        _activeConnections = _meter.CreateObservableGauge<long>(
            "messaging.connections.active", () => _activeConnectionCount);
    }

    public void EventPublished() => _eventsPublished.Add(1);
    public void EventPublishFailed() => _eventsPublishFailed.Add(1);
    public void EventDelivered() => _eventsDelivered.Add(1);
    public void EventDeliveryFailed() => _eventsDeliveryFailed.Add(1);
    public void EventDeadLettered() => _eventsDeadLettered.Add(1);
    public void CircuitBreakerTripped() => _circuitBreakerTripped.Add(1);
    public void CircuitBreakerReset() => _circuitBreakerReset.Add(1);
    public void RecordPublishDuration(double ms) => _publishDuration.Record(ms);
    public void RecordDeliveryDuration(double ms) => _deliveryDuration.Record(ms);
    public void ConnectionOpened() => Interlocked.Increment(ref _activeConnectionCount);
    public void ConnectionClosed() => Interlocked.Decrement(ref _activeConnectionCount);

    public void Dispose()
    {
        _meter.Dispose();
    }
}
