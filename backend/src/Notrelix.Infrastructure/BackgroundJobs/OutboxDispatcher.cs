using System.Text.Json;
using Notrelix.Infrastructure.Data;

using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class OutboxDispatcher : BackgroundService
{
    private const string DispatcherConsumerName = "OutboxDispatcher";
    private const int BatchSize = 20;
    private const int PollIntervalMs = 5000;
    private const int ProcessingTimeoutSeconds = 60;
    private const int MaxRetries = 5;
    private const int MaxBackoffSeconds = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher failed");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
        var eventCatalog = scope.ServiceProvider.GetRequiredService<IIntegrationEventCatalog>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);

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
                        (status = 'Failed' AND next_attempt_at <= {0})
                    )
                    ORDER BY created_at
                    LIMIT {2}
                    FOR UPDATE SKIP LOCKED
                """, now.UtcDateTime, processingCutoff.UtcDateTime, BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            foreach (var message in messages)
            {
                message.MarkProcessing(now);
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (var message in messages)
            {
                await ProcessMessageAsync(message, context, eventTypeRegistry, eventCatalog, integrationEventBus, dateTimeProvider, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
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
            integrationEventType = eventCatalog.Resolve(message.MessageName);
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

            await integrationEventBus.PublishAsync(integrationEvent, cancellationToken);

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
