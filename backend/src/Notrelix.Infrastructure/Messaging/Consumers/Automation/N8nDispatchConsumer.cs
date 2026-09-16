using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Inbound adapter for a durable <see cref="N8nDispatchRequestedV1"/>.
/// Owns a consumer-scoped database transaction (with RLS) so the Automation
/// attempt evidence is committed atomically — including on retryable failure.
/// The consumer owns no Automation state and no business rules.
/// </summary>
public sealed class N8nDispatchConsumer : IConsumer<N8nDispatchRequestedV1>
{
    private readonly N8nDispatchUseCase _useCase;
    private readonly ApplicationDbContext _db;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<N8nDispatchConsumer> _logger;
    private readonly PipelineMetrics _metrics;

    public N8nDispatchConsumer(
        N8nDispatchUseCase useCase,
        ApplicationDbContext db,
        IRlsSessionContext rls,
        ILogger<N8nDispatchConsumer> logger,
        PipelineMetrics? metrics = null)
    {
        _useCase = useCase;
        _db = db;
        _rls = rls;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task Consume(ConsumeContext<N8nDispatchRequestedV1> context)
    {
        var message = context.Message;

        await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            await _rls.ApplyAsync(context.CancellationToken);

            var dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var complete = await _useCase.ExecuteAsync(message, context.CancellationToken);
            _metrics.N8nDispatchDuration.Record(dispatchStopwatch.Elapsed.TotalMilliseconds);

            if (complete)
            {
                await _db.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);
                _metrics.N8nDispatchSucceeded.Add(1);
                return;
            }

            // Retryable provider outcome: commit the evidence (status re-queued,
            // attempt count incremented) so the durable state survives MassTransit
            // retry, then signal the delivery mechanism to redeliver.
            await _db.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

            _metrics.N8nDispatchFailed.Add(1);
            _metrics.N8nDispatchRetries.Add(1);
            throw new N8nDispatchRetryableException(message.ExecutionId);
        }
        catch (N8nDispatchRetryableException)
        {
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
}

/// <summary>
/// Signals the delivery mechanism to redeliver the dispatch intent. Carries the
/// stable execution identity for diagnostics only.
/// </summary>
public sealed class N8nDispatchRetryableException(Guid executionId)
    : InvalidOperationException($"n8n dispatch for execution {executionId} requires another delivery attempt.");
