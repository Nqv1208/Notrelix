using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Reliability;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Transport;

namespace Notrelix.Platform.Messaging.Host;

public sealed class MessagingHost : IMessagingHost, IAsyncDisposable
{
    private readonly IMessagingRuntime _runtime;
    private readonly IDeliveryEngine _deliveryEngine;
    private readonly IConnectionManager _connectionManager;
    private readonly MessagingHostOptions _options;
    private readonly MessagingMetrics _metrics;
    private readonly IDiagnosticEventPublisher _diagnosticEvents;
    private readonly MessagingHealthCheck _healthCheck;
    private readonly ILogger<MessagingHost>? _logger;
    private readonly CancellationTokenSource _shutdownCts = new();

    private int _disposed;

    public MessagingHost(
        IMessagingRuntime runtime,
        IDeliveryEngine deliveryEngine,
        IConnectionManager connectionManager,
        IOptions<MessagingHostOptions> options,
        MessagingMetrics metrics,
        IDiagnosticEventPublisher diagnosticEvents,
        MessagingHealthCheck healthCheck,
        ILogger<MessagingHost>? logger = null)
    {
        _runtime = runtime;
        _deliveryEngine = deliveryEngine;
        _connectionManager = connectionManager;
        _options = options.Value;
        _metrics = metrics;
        _diagnosticEvents = diagnosticEvents;
        _healthCheck = healthCheck;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("MessagingHost starting");

        if (_options.AutoConnect)
        {
            await ConnectTransportAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("MessagingHost stopping");

        await _shutdownCts.CancelAsync();

        if (_connectionManager.IsConnected)
        {
            await _connectionManager.DisconnectAsync(cancellationToken);
            _metrics.ConnectionClosed();
        }
    }

    public async Task<MessagingResult> PublishAsync(
        EventPublication publication,
        CancellationToken cancellationToken = default)
    {
        var sw = ValueStopwatch.StartNew();

        try
        {
            var result = await _runtime.PublishAsync(publication, cancellationToken);

            if (result.Success)
            {
                _metrics.EventPublished();
                _metrics.RecordPublishDuration(sw.Elapsed.TotalMilliseconds);
                _diagnosticEvents.Publish(new EventPublishedEvent
                {
                    EventName = publication.Event.GetType().Name,
                    EnvelopeId = result.EnvelopeId.ToString(),
                    DurationMs = (long)sw.Elapsed.TotalMilliseconds,
                });
            }
            else
            {
                _metrics.EventPublishFailed();
                _diagnosticEvents.Publish(new EventPublishFailedEvent
                {
                    EventName = publication.Event.GetType().Name,
                    Error = string.Join("; ", result.Errors ?? []),
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _metrics.EventPublishFailed();
            _diagnosticEvents.Publish(new EventPublishFailedEvent
            {
                EventName = publication.Event.GetType().Name,
                Error = ex.Message,
            });

            throw;
        }
    }

    public async Task<DeliveryResult> DeliverAsync(
        EventEnvelope envelope,
        Func<Task> sendAsync,
        DeliveryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = ValueStopwatch.StartNew();

        try
        {
            var result = await _deliveryEngine.DeliverAsync(envelope, sendAsync, options, cancellationToken);

            if (result.Success)
            {
                _metrics.EventDelivered();
                _metrics.RecordDeliveryDuration(sw.Elapsed.TotalMilliseconds);
                _diagnosticEvents.Publish(new DeliverySucceededEvent
                {
                    EventName = envelope.EventName,
                    Consumer = options?.ConsumerName ?? "",
                    RetryCount = result.RetryCount,
                });
            }
            else
            {
                _metrics.EventDeliveryFailed();
                if (result.DeadLettered)
                    _metrics.EventDeadLettered();

                _diagnosticEvents.Publish(new DeliveryFailedEvent
                {
                    EventName = envelope.EventName,
                    Consumer = options?.ConsumerName ?? "",
                    Error = result.ErrorMessage ?? "",
                    DeadLettered = result.DeadLettered,
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _metrics.EventDeliveryFailed();
            _diagnosticEvents.Publish(new DeliveryFailedEvent
            {
                EventName = envelope.EventName,
                Consumer = options?.ConsumerName ?? "",
                Error = ex.Message,
            });

            throw;
        }
    }

    public async Task<MessagingHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return await _healthCheck.CheckAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _shutdownCts.Cancel();
        _shutdownCts.Dispose();

        if (_connectionManager.IsConnected)
        {
            await _connectionManager.DisconnectAsync(CancellationToken.None);
        }

        _metrics.Dispose();
    }

    private async Task ConnectTransportAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.ConnectRetryCount; attempt++)
        {
            try
            {
                await _connectionManager.ConnectAsync(cancellationToken);
                _metrics.ConnectionOpened();
                _logger?.LogInformation("Messaging transport connected");
                return;
            }
            catch (Exception ex) when (attempt < _options.ConnectRetryCount)
            {
                _logger?.LogWarning(ex,
                    "Messaging transport connection attempt {Attempt}/{MaxAttempts} failed",
                    attempt, _options.ConnectRetryCount);
                await Task.Delay(_options.ConnectRetryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Messaging transport connection failed after {MaxAttempts} attempts",
                    _options.ConnectRetryCount);
                throw;
            }
        }
    }
}
