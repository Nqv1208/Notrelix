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
    private readonly IServiceProvider? _serviceProvider;
    private readonly ConcurrentDictionary<string, ConsumerRegistration> _registrations = new();

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, PoisonDetector> _poisonDetectors = new();
    private readonly ConcurrentDictionary<string, OrderingEnforcer> _orderingEnforcers = new();
    private readonly CancellationTokenSource _shutdownCts = new();

    private int _disposed;

    public ConsumerHost(
        MessagingMetrics metrics,
        IDiagnosticEventPublisher diagnosticEvents,
        ILogger<ConsumerHost>? logger = null,
        IServiceProvider? serviceProvider = null)
    {
        _metrics = metrics;
        _diagnosticEvents = diagnosticEvents;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void Register(
        string eventName,
        Func<EventEnvelope, CancellationToken, Task> handler,
        Action<ConsumerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var options = new ConsumerOptions();
        configure?.Invoke(options);

        Register(new ConsumerRegistration
        {
            EventName = eventName,
            Handler = handler,
            Options = options,
        });
    }

    public void Register(ConsumerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var eventName = registration.EventName;
        var options = registration.Options;

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
            // Disabled: no delivery, no success metric, no failure — by design.
            _logger?.LogDebug("Consumer for {EventName} is disabled", envelope.EventName);
            return;
        }

        if (registration.Options.ExpectedEventVersion is int expectedVersion
            && envelope.EventVersion != expectedVersion)
        {
            _logger?.LogWarning("Event version mismatch for {EventName}: expected {Expected}, got {Actual}",
                envelope.EventName, expectedVersion, envelope.EventVersion);
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = $"Event version mismatch: expected {expectedVersion}, got {envelope.EventVersion}",
            });
            throw new MessageContractException(
                $"Event version mismatch for '{envelope.EventName}': expected {expectedVersion}, got {envelope.EventVersion}.");
        }

        OrderingLease? orderingLease = null;

        if (registration.Options.OrderingRequired)
        {
            var partitionKey = GetOrderingPartitionKey(envelope);
            var orderingEnforcer = _orderingEnforcers[envelope.EventName];

            // Ordering requires a real envelope sequence. A missing sequence is a
            // contract violation — never synthesized. A fabricated value would
            // silently reorder or acknowledge messages the transport cannot order,
            // so the delivery fails observably for transport retry.
            if (envelope.Sequence is null)
            {
                _logger?.LogWarning("Ordering requires a sequence for {EventName} (partition {Partition}); envelope {Id} has none",
                    envelope.EventName, partitionKey, envelope.Id);
                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Error = "Ordering requires a sequence",
                });
                throw new MessageOrderingException(
                    $"Ordering for '{envelope.EventName}' partition '{partitionKey}' requires a sequence; " +
                    $"envelope '{envelope.Id}' has none.");
            }

            // D-003: the partition lease is acquired before the event semaphore so
            // one partition cannot occupy every global slot while waiting for its
            // gate, starving other partitions.
            var acquisition = await orderingEnforcer.AcquireAsync(
                partitionKey, envelope.Sequence.Value, cancellationToken);

            if (acquisition.Outcome != OrderingAcquisitionOutcome.Allowed)
            {
                // Denied acquisition bypasses the handler-failure/poison catch and
                // does not acquire the global semaphore.
                _logger?.LogWarning("Ordering rejected {EventName} ({Partition}): {Outcome} (expected {Expected}, received {Received})",
                    envelope.EventName, partitionKey, acquisition.Outcome,
                    acquisition.ExpectedSequence, acquisition.ReceivedSequence);
                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Error = $"Ordering: {acquisition.Outcome} (expected {acquisition.ExpectedSequence}, received {acquisition.ReceivedSequence})",
                });
                throw new MessageOrderingException(
                    $"Ordering rejected for '{envelope.EventName}' partition '{partitionKey}': {acquisition.Outcome} " +
                    $"(expected {acquisition.ExpectedSequence}, received {acquisition.ReceivedSequence}).");
            }

            orderingLease = acquisition.Lease;
        }

        var semaphore = _semaphores[envelope.EventName];
        var poisonDetector = _poisonDetectors[envelope.EventName];

        // Wait for a concurrency slot up to the configured timeout. Never drop: when
        // the wait expires the dispatch fails observably so the transport can retry.
        if (!await semaphore.WaitAsync(registration.Options.QueueWaitTimeout, cancellationToken))
        {
            if (orderingLease is not null)
            {
                await orderingLease.DisposeAsync();
            }

            _logger?.LogWarning("Concurrency backpressure timeout for {EventName} after {Timeout} (message {Id})",
                envelope.EventName, registration.Options.QueueWaitTimeout, envelope.Id);
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = "Concurrency backpressure timeout",
            });
            throw new ConsumerBackpressureException(
                $"Consumer for '{envelope.EventName}' did not acquire a concurrency slot within " +
                $"{registration.Options.QueueWaitTimeout.TotalSeconds:0.#}s; the message was not dropped.");
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _shutdownCts.Token, cancellationToken);

            if (registration.ScopedHandler is not null)
            {
                var provider = _serviceProvider ?? throw new InvalidOperationException(
                    $"Consumer for '{envelope.EventName}' requires a service provider to create dispatch scopes.");
                await registration.ScopedHandler(provider, envelope, linkedCts.Token);
            }
            else
            {
                await registration.Handler(envelope, linkedCts.Token);
            }

            // The sequence is committed only after handler success; a failed
            // handler leaves the sequence uncommitted so a retry of the same
            // message is accepted.
            orderingLease?.Commit();
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
            // Diagnostics first, then the failure stays observable: below the poison
            // threshold the original exception is rethrown for transport retry; at the
            // threshold a typed dead-letter recommendation wraps it. Handler failures
            // are never swallowed into a false delivery success.
            _logger?.LogError(ex, "Consumer {EventName} failed processing {Id}", envelope.EventName, envelope.Id);
            var detection = poisonDetector.RecordFailure(envelope.EventName);

            _metrics.EventDeliveryFailed();
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Error = ex.Message,
            });

            if (detection.IsPoison)
            {
                _logger?.LogWarning("Consumer {EventName} detected as poison after {Count} failures (threshold {Threshold})",
                    envelope.EventName, detection.CurrentPoisonCount, detection.Threshold);
                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Error = "Poison detected",
                    DeadLettered = true,
                });
                throw new PoisonMessageException(
                    $"Consumer for '{envelope.EventName}' failed {detection.CurrentPoisonCount} times " +
                    $"(threshold {detection.Threshold}); dead-letter recommended.", ex);
            }

            throw;
        }
        finally
        {
            if (orderingLease is not null)
            {
                await orderingLease.DisposeAsync();
            }

            semaphore.Release();
        }
    }

    public IReadOnlyList<ConsumerRegistration> GetRegistrations()
    {
        return _registrations.Values.ToList();
    }

    private static string GetOrderingPartitionKey(EventEnvelope envelope) =>
        envelope.AggregateId is Guid aggregateId
            ? $"aggregate:{aggregateId:N}"
            : $"correlation:{envelope.CorrelationId:N}";

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
