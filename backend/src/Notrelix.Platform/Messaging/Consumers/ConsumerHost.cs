using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Reliability;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

public sealed class ConsumerHost : IConsumerHost, IAsyncDisposable
{
    private readonly MessagingMetrics _metrics;
    private readonly IDiagnosticEventPublisher _diagnosticEvents;
    private readonly ILogger<ConsumerHost>? _logger;
    private readonly ConcurrentDictionary<string, ConsumerRegistration> _registrations = new();

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, PoisonDetector> _poisonDetectors = new();
    private readonly ConcurrentDictionary<string, OrderingEnforcer> _orderingEnforcers = new();
    private readonly CancellationTokenSource _shutdownCts = new();

    private int _disposed;

    public ConsumerHost(
        MessagingMetrics metrics,
        IDiagnosticEventPublisher diagnosticEvents,
        ILogger<ConsumerHost>? logger = null)
    {
        _metrics = metrics;
        _diagnosticEvents = diagnosticEvents;
        _logger = logger;
    }

    public void Register(
        string eventName,
        Func<EventEnvelope, CancellationToken, Task> handler,
        Action<ConsumerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var options = new ConsumerOptions();
        configure?.Invoke(options);

        var registration = new ConsumerRegistration
        {
            EventName = eventName,
            Handler = handler,
            Options = options,
        };

        if (!_registrations.TryAdd(eventName, registration))
        {
            _logger?.LogWarning("Consumer already registered for event {EventName}, overwriting", eventName);
            _registrations[eventName] = registration;
        }

        _semaphores[eventName] = new SemaphoreSlim(options.ConcurrencyLimit, options.ConcurrencyLimit);
        _poisonDetectors[eventName] = new PoisonDetector(options.PoisonThreshold);
        _orderingEnforcers[eventName] = new OrderingEnforcer();

        _logger?.LogInformation("Registered consumer for {EventName} (concurrency={Concurrency}, ordering={Ordering})",
            eventName, options.ConcurrencyLimit, options.OrderingRequired);
    }

    public async Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!_registrations.TryGetValue(envelope.EventName, out var registration))
        {
            _logger?.LogDebug("No consumer registered for {EventName}", envelope.EventName);
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = "No consumer registered",
            });
            return;
        }

        if (!registration.Options.Enabled)
        {
            _logger?.LogDebug("Consumer for {EventName} is disabled", envelope.EventName);
            return;
        }

        var partitionKey = registration.Options.OrderingRequired
            ? envelope.AggregateId?.ToString() ?? envelope.CorrelationId.ToString()
            : null;

        if (partitionKey is not null)
        {
            var orderingEnforcer = _orderingEnforcers[envelope.EventName];
            var orderingResult = orderingEnforcer.ValidateSequence(partitionKey, 0);

            if (!orderingResult.CanProcess)
            {
                _logger?.LogWarning("Ordering rejected {EventName} ({Partition}): {Reason}",
                    envelope.EventName, partitionKey, orderingResult.Reason);
                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Error = $"Ordering: {orderingResult.Reason}",
                });
                return;
            }
        }

        var semaphore = _semaphores[envelope.EventName];
        var poisonDetector = _poisonDetectors[envelope.EventName];

        if (!await semaphore.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            _logger?.LogWarning("Concurrency limit reached for {EventName}, dropping message {Id}",
                envelope.EventName, envelope.Id);
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = "Concurrency limit reached",
            });
            return;
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _shutdownCts.Token, cancellationToken);

            await registration.Handler(envelope, linkedCts.Token);

            poisonDetector.Reset(envelope.EventName);
            _metrics.EventDelivered();
            _diagnosticEvents.Publish(new DeliverySucceededEvent
            {
                EventName = envelope.EventName,
            });
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger?.LogInformation("Consumer {EventName} shutting down", envelope.EventName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Consumer {EventName} failed processing {Id}", envelope.EventName, envelope.Id);
            poisonDetector.RecordFailure(envelope.EventName);

            _metrics.EventDeliveryFailed();
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = ex.Message,
            });

            if (poisonDetector.GetPoisonCount(envelope.EventName) >= registration.Options.PoisonThreshold)
            {
                _logger?.LogWarning("Consumer {EventName} detected as poison after failures",
                    envelope.EventName);
                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Error = "Poison detected",
                    DeadLettered = true,
                });
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public IReadOnlyList<ConsumerRegistration> GetRegistrations()
    {
        return _registrations.Values.ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _shutdownCts.CancelAsync();

        foreach (var sem in _semaphores.Values)
            sem.Dispose();

        _shutdownCts.Dispose();
    }
}
