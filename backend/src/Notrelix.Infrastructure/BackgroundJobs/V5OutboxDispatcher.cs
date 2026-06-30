using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class V5OutboxDispatcher : BackgroundService
{
    private const string DispatcherConsumerName = "V5OutboxDispatcher";
    private const int BatchSize = 20;
    private const int PollIntervalMs = 5000;
    private const int ProcessingTimeoutSeconds = 60;
    private const int MaxRetries = 5;
    private const int MaxBackoffSeconds = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<V5OutboxDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public V5OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<V5OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("V5OutboxDispatcher started");

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
                _logger.LogError(ex, "V5OutboxDispatcher failed");
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
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);

        var messages = await context.Set<MessagingOutboxMessage>()
            .FromSqlRaw("""
                SELECT * FROM messaging.outbox_messages
                WHERE (
                    ("Status" = 'Pending' AND "NextAttemptAt" <= {0})
                    OR
                    ("Status" = 'Processing' AND "ProcessingStartedAt" <= {1})
                    OR
                    ("Status" = 'Failed' AND "NextAttemptAt" <= {0})
                )
                ORDER BY "CreatedAt"
                LIMIT {2}
                FOR UPDATE SKIP LOCKED
            """, now.UtcDateTime, processingCutoff.UtcDateTime, BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            message.MarkProcessing(now);
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, context, eventTypeRegistry, integrationEventBus, dateTimeProvider, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        MessagingOutboxMessage message,
        ApplicationDbContext context,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventBus integrationEventBus,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        var alreadyProcessed = await context.Set<MessagingProcessedEvent>()
            .AnyAsync(x => x.EventId == message.EventId && x.ConsumerName == DispatcherConsumerName, cancellationToken);

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

        var integrationEventType = eventTypeRegistry.GetEventType(message.MessageName);

        if (integrationEventType is null)
        {
            _logger.LogWarning("V5 outbox {MsgId}: event type {MsgName} not found in registry",
                message.Id, message.MessageName);
            FailMessage(message, attempt, "EventTypeNotFound", "EventType not found in registry: " + message.MessageName, dateTimeProvider);
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
