using System.Text.Json;
using Notrelix.Infrastructure.Data;

using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class OutboxDispatcher : BackgroundService
{
    private const string DispatcherConsumerName = "OutboxDispatcher";
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int ProcessingTimeoutSeconds = 60;
    private const int MaxRetries = 5;
    private const int MaxBackoffSeconds = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IOutboxWakeSignal _wakeSignal;
    private readonly MetricsService _metrics;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DateTimeOffset _lastCountRefresh;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger,
        IOutboxWakeSignal wakeSignal,
        MetricsService metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _wakeSignal = wakeSignal;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessBatchAsync(stoppingToken))
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher failed");
            }

            await _wakeSignal.WaitAsync(PollInterval, stoppingToken);
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await RefreshOutboxCountsIfDueAsync(cancellationToken);

        // Phase 1: Short claim transaction — select + mark Processing + commit
        var (claimed, lockId) = await ClaimBatchAsync(cancellationToken);
        if (claimed.Count == 0) return false;

        // Phase 2: Publish outside database transaction
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
        var eventCatalog = scope.ServiceProvider.GetRequiredService<IIntegrationEventCatalog>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // Re-attach claimed messages to this scope's context
        var messages = await context.Set<MessagingOutboxMessage>()
            .Where(m => claimed.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            // Verify lease ownership before processing
            if (message.LockId != lockId)
            {
                _logger.LogWarning(
                    "Outbox {MsgId}: lease lost (expected {ExpectedLock}, found {ActualLock}). Skipping.",
                    message.Id, lockId, message.LockId);
                continue;
            }

            await ProcessMessageAsync(message, context, eventTypeRegistry, eventCatalog, integrationEventBus, dateTimeProvider, cancellationToken);
        }

        // Phase 3: Short completion transaction — mark Processed/Failed + commit
        await context.SaveChangesAsync(cancellationToken);
        return claimed.Count == BatchSize;
    }

    private async Task RefreshOutboxCountsIfDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastCountRefresh < TimeSpan.FromSeconds(30))
        {
            return;
        }

        _lastCountRefresh = now;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Dead-letter is the exhausted 'Failed' terminal state, not a status of
            // its own: retry_count >= max_retries marks rows no dispatcher will claim.
            var counts = await context.Set<MessagingOutboxMessage>()
                .GroupBy(m => new { m.Status, Exhausted = m.RetryCount >= m.MaxRetries })
                .Select(g => new { g.Key.Status, g.Key.Exhausted, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var oldestUndispatched = await context.Set<MessagingOutboxMessage>()
                .Where(m => m.Status == "Pending" || m.Status == "Processing" || m.Status == "Failed")
                .MinAsync(m => (DateTimeOffset?)m.CreatedAt, cancellationToken);

            _metrics.UpdateOutboxCounts(
                counts.Where(c => c.Status == "Pending").Sum(c => c.Count),
                counts.Where(c => c.Status == "Failed").Sum(c => c.Count),
                counts.Where(c => c.Status == "Failed" && c.Exhausted).Sum(c => c.Count),
                oldestUndispatched.HasValue ? (now - oldestUndispatched.Value).TotalMilliseconds : null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh outbox gauge counts");
        }
    }

    private async Task<(List<Guid> Ids, Guid LockId)> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);
        var lockId = Guid.NewGuid();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var messages = await context.Set<MessagingOutboxMessage>()
                .FromSqlRaw("""
                    SELECT * FROM messaging.outbox_messages
                    WHERE (
                        (status = 'Pending' AND next_attempt_at <= {0})
                        OR
                        (status = 'Processing' AND processing_started_at <= {1})
                        OR
                        (status = 'Failed' AND retry_count < max_retries AND next_attempt_at <= {0})
                    )
                    AND (
                        stream_key IS NULL
                        OR NOT EXISTS (
                            SELECT 1
                            FROM messaging.outbox_messages earlier
                            WHERE earlier.stream_key = outbox_messages.stream_key
                              AND earlier.stream_version < outbox_messages.stream_version
                              AND earlier.status <> 'Processed'
                        )
                    )
                    ORDER BY created_at
                    LIMIT {2}
                    FOR UPDATE SKIP LOCKED
                """, now.UtcDateTime, processingCutoff.UtcDateTime, BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ([], Guid.Empty);
            }

            foreach (var message in messages)
            {
                message.MarkProcessing(now, lockId);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            foreach (var message in messages)
            {
                _metrics.CommitToClaim.Record((now - message.CreatedAt).TotalMilliseconds);
            }

            _logger.LogDebug("Claimed {Count} outbox messages for dispatch (lock={LockId})", messages.Count, lockId);
            return (messages.Select(m => m.Id).ToList(), lockId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessMessageAsync(
        MessagingOutboxMessage message,
        ApplicationDbContext context,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventCatalog eventCatalog,
        IIntegrationEventBus integrationEventBus,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        var alreadyProcessed = await context.Set<MessagingProcessedEvent>()
            .AnyAsync(x => x.EventId == message.EventId
                && x.ConsumerName == DispatcherConsumerName
                && x.Status == "Succeeded", cancellationToken);

        if (alreadyProcessed)
        {
            message.MarkProcessed(now);
            _logger.LogDebug("V5 outbox {MsgId}: {MsgName} already processed, skipping",
                message.Id, message.MessageName);
            return;
        }

        var attempt = new OutboxDeliveryAttempt(
            message.Id, message.EventId, message.RetryCount + 1,
            Environment.MachineName, "MassTransit", null, "Started", now);
        context.Set<OutboxDeliveryAttempt>().Add(attempt);

        Type integrationEventType;
        try
        {
            // Compound contract identity: the outbox row carries both the logical
            // message name and its schema version (IAREQ131). No name-only fallback.
            integrationEventType = eventCatalog.Resolve(new EventContractKey(message.MessageName, message.SchemaVersion));
        }
        catch (UnknownIntegrationEventTypeException ex)
        {
            _logger.LogCritical(ex, "V5 outbox {MsgId}: unknown integration event type {MsgName} — dead-lettering permanently",
                message.Id, message.MessageName);
            message.MarkDeadLetter("UnknownEventType", ex.Message, now);
            attempt.MarkFailed("UnknownEventType", ex.Message, now);
            var processedEvent = new MessagingProcessedEvent(
                message.EventId, DispatcherConsumerName,
                message.SourceContext, message.MessageName, message.SchemaVersion,
                message.SourceEventId, message.SubjectType, message.SubjectId,
                message.WorkspaceId, message.ActorUserId,
                message.CorrelationId, message.CausationId, now);
            processedEvent.MarkFailed(now, ex.Message);
            context.Set<MessagingProcessedEvent>().Add(processedEvent);
            return;
        }

        try
        {
            var integrationEvent = message.PayloadJson.Deserialize(integrationEventType, JsonOptions) as IIntegrationEvent;

            if (integrationEvent is null)
            {
                _logger.LogError("V5 outbox {MsgId}: failed to deserialize payload as {MsgName}",
                    message.Id, message.MessageName);
                FailMessage(message, attempt, "DeserializationFailed", "Deserialization returned null or unexpected type", dateTimeProvider);
                return;
            }

            // Publish OUTSIDE database transaction — broker latency does not hold DB locks
            var publishStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await integrationEventBus.PublishAsync(integrationEvent, cancellationToken);
            publishStopwatch.Stop();

            var publishedAt = dateTimeProvider.UtcNow;
            _metrics.OutboxPublishDuration.Record(publishStopwatch.Elapsed.TotalMilliseconds);
            _metrics.CommitToPublish.Record((publishedAt - message.CreatedAt).TotalMilliseconds);
            _metrics.OutboxDispatchedCount.Add(1);

            message.MarkProcessed(now);
            attempt.MarkSucceeded(now);

            var processedEvent = new MessagingProcessedEvent(
                message.EventId, DispatcherConsumerName,
                message.SourceContext, message.MessageName, message.SchemaVersion,
                message.SourceEventId, message.SubjectType, message.SubjectId,
                message.WorkspaceId, message.ActorUserId,
                message.CorrelationId, message.CausationId, now);
            processedEvent.MarkSucceeded(now);
            context.Set<MessagingProcessedEvent>().Add(processedEvent);

            _logger.LogDebug("V5 outbox {MsgId}: {MsgName} dispatched (attempt {Retry})",
                message.Id, message.MessageName, message.RetryCount + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V5 outbox {MsgId}: {MsgName} failed (attempt {Retry})",
                message.Id, message.MessageName, message.RetryCount + 1);
            _metrics.PublishFailures.Add(1);
            FailMessage(message, attempt, "DispatchFailed", ex.ToString(), dateTimeProvider);
        }
    }

    private static void FailMessage(
        MessagingOutboxMessage message,
        OutboxDeliveryAttempt attempt,
        string errorCode,
        string errorMessage,
        IDateTimeProvider dateTimeProvider)
    {
        message.MarkFailed(errorCode, errorMessage, dateTimeProvider.UtcNow);
        attempt.MarkFailed(errorCode, errorMessage, dateTimeProvider.UtcNow);
    }
}
