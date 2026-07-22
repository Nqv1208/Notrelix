using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Reliability;

public sealed record DeliveryOptions
{
    public string? ConsumerName { get; init; }
    public string? PartitionKey { get; init; }
    public long? SequenceNumber { get; init; }
    public RetryPolicy? RetryPolicy { get; init; }
}

public sealed record DeliveryResult
{
    public bool Success { get; init; }
    public bool DeadLettered { get; init; }
    public int RetryCount { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string>? Warnings { get; init; }

    public static DeliveryResult Ok(int retryCount = 0) =>
        new() { Success = true, RetryCount = retryCount };

    public static DeliveryResult Failed(string error, int retryCount = 0) =>
        new() { Success = false, ErrorMessage = error, RetryCount = retryCount };

    public static DeliveryResult DeadLetter(string error, int retryCount = 0) =>
        new() { Success = false, DeadLettered = true, ErrorMessage = error, RetryCount = retryCount };
}

public interface IDeliveryEngine
{
    Task<DeliveryResult> DeliverAsync(
        EventEnvelope envelope,
        Func<Task> sendAction,
        DeliveryOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class DeliveryEngine : IDeliveryEngine
{
    private readonly RetryPolicy _defaultRetryPolicy;
    private readonly PoisonDetector _poisonDetector;
    private readonly OrderingEnforcer _orderingEnforcer;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IDeadLetterQueue? _deadLetterQueue;
    private readonly ILogger<DeliveryEngine>? _logger;

    public DeliveryEngine(
        PoisonDetector poisonDetector,
        OrderingEnforcer orderingEnforcer,
        CircuitBreaker circuitBreaker,
        IDeadLetterQueue? deadLetterQueue = null,
        RetryPolicy? defaultRetryPolicy = null,
        ILogger<DeliveryEngine>? logger = null)
    {
        _poisonDetector = poisonDetector;
        _orderingEnforcer = orderingEnforcer;
        _circuitBreaker = circuitBreaker;
        _deadLetterQueue = deadLetterQueue;
        _defaultRetryPolicy = defaultRetryPolicy ?? RetryPolicy.Default;
        _logger = logger;
    }

    public async Task<DeliveryResult> DeliverAsync(
        EventEnvelope envelope,
        Func<Task> sendAction,
        DeliveryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DeliveryOptions();
        var retryPolicy = options.RetryPolicy ?? _defaultRetryPolicy;
        var circuitName = options.ConsumerName ?? envelope.EventName;

        if (!_circuitBreaker.IsRequestAllowed(circuitName))
        {
            _logger?.LogWarning("Circuit breaker is open for {Circuit}", circuitName);
            return DeliveryResult.Failed($"Circuit breaker open for {circuitName}");
        }

        if (options.PartitionKey is not null && options.SequenceNumber.HasValue)
        {
            var orderingResult = _orderingEnforcer.ValidateSequence(
                options.PartitionKey, options.SequenceNumber.Value);

            if (!orderingResult.CanProcess)
            {
                _logger?.LogWarning(
                    "Ordering enforcement failed for {Event}: {Reason}",
                    envelope.EventName, orderingResult.Reason);
                return DeliveryResult.Failed($"Ordering: {orderingResult.Reason}");
            }
        }

        var retryCount = 0;
        while (retryCount <= retryPolicy.MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await sendAction();

                _circuitBreaker.RecordSuccess(circuitName);
                _poisonDetector.Reset(envelope.EventName, options.ConsumerName);

                return DeliveryResult.Ok(retryCount);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Delivery failed for {Event} (attempt {Attempt}/{Max})",
                    envelope.EventName, retryCount + 1, retryPolicy.MaxRetries + 1);

                _circuitBreaker.RecordFailure(circuitName);

                var poisonResult = _poisonDetector.RecordFailure(
                    envelope.EventName, options.ConsumerName);

                if (poisonResult.IsPoison)
                {
                    _logger?.LogError(
                        "Event {Event} detected as poison (count: {Count}/{Threshold})",
                        envelope.EventName, poisonResult.CurrentPoisonCount, poisonResult.Threshold);

                    await DeadLetterAsync(envelope, ex, retryCount, poisonResult.CurrentPoisonCount);
                    return DeliveryResult.DeadLetter(
                        $"Poison threshold exceeded: {ex.Message}", retryCount);
                }

                if (!retryPolicy.ShouldRetry(retryCount, ex))
                {
                    await DeadLetterAsync(envelope, ex, retryCount, poisonResult.CurrentPoisonCount);
                    return DeliveryResult.DeadLetter(
                        $"Retry exhausted: {ex.Message}", retryCount);
                }

                retryCount++;
                var delay = retryPolicy.GetDelay(retryCount);
                _logger?.LogDebug("Retrying {Event} in {Delay}ms", envelope.EventName, delay.TotalMilliseconds);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }
        }

        return DeliveryResult.Failed("Max retries exceeded");
    }

    private async Task DeadLetterAsync(
        EventEnvelope envelope, Exception ex, int retryCount, int poisonCount)
    {
        if (_deadLetterQueue is null)
            return;

        var entry = new DeadLetterEntry
        {
            Id = Guid.NewGuid(),
            EventName = envelope.EventName,
            EventVersion = envelope.EventVersion,
            Payload = envelope.Data.ToArray(),
            Reason = ex.Message,
            RetryCount = retryCount,
            PoisonCount = poisonCount,
            OccurredAt = envelope.OccurredAt,
            DeadLetteredAt = DateTimeOffset.UtcNow,
            CorrelationId = envelope.CorrelationId,
            WorkspaceId = envelope.WorkspaceId,
        };

        await _deadLetterQueue.DeadLetterAsync(entry);
    }
}
