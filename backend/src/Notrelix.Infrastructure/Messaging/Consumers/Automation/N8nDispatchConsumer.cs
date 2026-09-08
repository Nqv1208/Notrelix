using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Thin inbound adapter for a durable <see cref="N8nDispatchRequestedV1"/>.
/// Establishes the execution/correlation context, delegates the business
/// progression to the Application-owned <see cref="N8nDispatchUseCase"/>, and
/// maps a retryable technical outcome onto the delivery mechanism's retry
/// contract (at-least-once). The consumer owns no Automation state and no
/// business rules.
/// </summary>
public sealed class N8nDispatchConsumer : IConsumer<N8nDispatchRequestedV1>
{
    private readonly N8nDispatchUseCase _useCase;
    private readonly ILogger<N8nDispatchConsumer> _logger;
    private readonly PipelineMetrics _metrics;

    public N8nDispatchConsumer(
        N8nDispatchUseCase useCase,
        ILogger<N8nDispatchConsumer> logger,
        PipelineMetrics? metrics = null)
    {
        _useCase = useCase;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task Consume(ConsumeContext<N8nDispatchRequestedV1> context)
    {
        var message = context.Message;

        var dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var complete = await _useCase.ExecuteAsync(message, context.CancellationToken);
        _metrics.N8nDispatchDuration.Record(dispatchStopwatch.Elapsed.TotalMilliseconds);

        if (complete)
        {
            _metrics.N8nDispatchSucceeded.Add(1);
            return;
        }

        // Retryable/unknown provider outcome: surface a technical retry so the
        // delivery mechanism redelivers the durable intent under the stable
        // ExecutionId (the receiving side deduplicates by that identity).
        _metrics.N8nDispatchFailed.Add(1);
        _metrics.N8nDispatchRetries.Add(1);
        throw new N8nDispatchRetryableException(message.ExecutionId);
    }
}

/// <summary>
/// Signals the delivery mechanism to redeliver the dispatch intent. Carries the
/// stable execution identity for diagnostics only.
/// </summary>
public sealed class N8nDispatchRetryableException(Guid executionId)
    : InvalidOperationException($"n8n dispatch for execution {executionId} requires another delivery attempt.");
