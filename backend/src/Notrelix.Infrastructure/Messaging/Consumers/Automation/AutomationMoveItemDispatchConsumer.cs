using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Thin inbound adapter for a durable <see cref="AutomationMoveItemRequestedV1"/>.
/// Delegates the business progression to the Application-owned
/// <see cref="AutomationMoveItemUseCase"/> and maps a retryable technical
/// outcome onto the delivery mechanism's retry contract (at-least-once). The
/// consumer owns no Automation state and no business rules.
/// </summary>
public sealed class AutomationMoveItemDispatchConsumer : IConsumer<AutomationMoveItemRequestedV1>
{
    private readonly AutomationMoveItemUseCase _useCase;
    private readonly ILogger<AutomationMoveItemDispatchConsumer> _logger;
    private readonly PipelineMetrics _metrics;

    public AutomationMoveItemDispatchConsumer(
        AutomationMoveItemUseCase useCase,
        ILogger<AutomationMoveItemDispatchConsumer> logger,
        PipelineMetrics? metrics = null)
    {
        _useCase = useCase;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task Consume(ConsumeContext<AutomationMoveItemRequestedV1> context)
    {
        var message = context.Message;
        var complete = await _useCase.ExecuteAsync(message, context.CancellationToken);

        if (complete)
        {
            return;
        }

        // Retryable technical target failure: surface a technical retry so the
        // delivery mechanism redelivers the durable intent under the stable
        // ExecutionId (the use case deduplicates by that identity).
        _logger.LogWarning(
            "Automation move-item dispatch for execution {ExecutionId} requires another delivery attempt.",
            message.ExecutionId);
        throw new AutomationMoveItemRetryableException(message.ExecutionId);
    }
}

/// <summary>
/// Signals the delivery mechanism to redeliver the dispatch intent. Carries the
/// stable execution identity for diagnostics only.
/// </summary>
public sealed class AutomationMoveItemRetryableException(Guid executionId)
    : InvalidOperationException($"automation move-item dispatch for execution {executionId} requires another delivery attempt.");
