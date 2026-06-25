using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Outbox;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class OutboxDispatcher : BackgroundService
{
    private const string OutboxDispatcherConsumerName = "OutboxDispatcher";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly MetricsService _metrics;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger,
        MetricsService metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
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

            await Task.Delay(OutboxDefaults.PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var processedEventStore = scope.ServiceProvider.GetRequiredService<IProcessedEventStore>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-OutboxDefaults.ProcessingTimeoutSeconds);

        var messages = await context.Set<OutboxMessage>()
            .FromSqlRaw("""
                UPDATE ops.outbox_messages
                SET status = 'Processing', processing_started_at = {2}
                WHERE id IN (
                    SELECT id FROM ops.outbox_messages
                    WHERE (
                        (status IN ('Pending', 'Failed') AND next_attempt_at <= {0})
                        OR
                        (status = 'Processing' AND processing_started_at <= {1})
                    )
                    ORDER BY created_at
                    LIMIT {3}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING *
            """, now.UtcDateTime, processingCutoff.UtcDateTime, now.UtcDateTime, OutboxDefaults.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, eventTypeRegistry, mediator, integrationEventBus, dateTimeProvider, processedEventStore, context, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        var counts = await context.Set<OutboxMessage>()
            .GroupBy(m => m.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var pending = counts.FirstOrDefault(c => c.Key == OutboxStatus.Pending)?.Count ?? 0;
        var failed = counts.FirstOrDefault(c => c.Key == OutboxStatus.Failed)?.Count ?? 0;
        var deadLetter = counts.FirstOrDefault(c => c.Key == OutboxStatus.DeadLetter)?.Count ?? 0;
        _metrics.UpdateOutboxCounts(pending, failed, deadLetter);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IEventTypeRegistry eventTypeRegistry,
        IMediator mediator,
        IIntegrationEventBus integrationEventBus,
        IDateTimeProvider dateTimeProvider,
        IProcessedEventStore processedEventStore,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        message.MarkProcessing(dateTimeProvider.UtcNow);

        if (await processedEventStore.IsProcessedAsync(message.EventId, OutboxDispatcherConsumerName, cancellationToken))
        {
            message.MarkProcessed(dateTimeProvider.UtcNow);
            _logger.LogDebug("Outbox message {Id}: {MessageName} already processed, skipping",
                message.Id, message.MessageName);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            switch (message.MessageType)
            {
                case OutboxMessageType.DomainEvent:
                    await DispatchDomainEventAsync(message, eventTypeRegistry, mediator, dateTimeProvider, cancellationToken);
                    break;
                case OutboxMessageType.IntegrationEvent:
                    await DispatchIntegrationEventAsync(message, eventTypeRegistry, integrationEventBus, dateTimeProvider, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Outbox message {Id}: unknown MessageType {MessageType}", message.Id, message.MessageType);
                    message.MarkProcessed(dateTimeProvider.UtcNow);
                    break;
            }

            var processedEvent = ProcessedEvent.Create(
                message.EventId,
                OutboxDispatcherConsumerName,
                message.MessageName,
                message.SchemaVersion,
                message.SourceEventId ?? message.EventId,
                message.WorkspaceId,
                dateTimeProvider.UtcNow);
            context.Set<ProcessedEvent>().Add(processedEvent);

            _metrics.OutboxDispatchedCount.Add(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox message {Id}: {MessageName} failed (attempt {Retry})",
                message.Id, message.MessageName, message.RetryCount + 1);
            message.MarkFailed(ex.ToString(), dateTimeProvider.UtcNow);
            _metrics.OutboxFailedDispatchCount.Add(1);
        }

        _metrics.OutboxDispatchDuration.Record(sw.Elapsed.TotalMilliseconds);
    }

    private async Task DispatchDomainEventAsync(
        OutboxMessage message,
        IEventTypeRegistry eventTypeRegistry,
        IMediator mediator,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var domainEventType = eventTypeRegistry.GetEventType(message.MessageName);

        if (domainEventType is null)
        {
            _logger.LogWarning("Outbox message {Id}: domain event type {MessageName} not found in registry", message.Id, message.MessageName);
            message.MarkFailed("EventType not found in registry: " + message.MessageName, dateTimeProvider.UtcNow);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(message.PayloadJson, domainEventType, JsonOptions) as IDomainEvent;

        if (domainEvent is null)
        {
            _logger.LogError("Outbox message {Id}: failed to deserialize payload as {MessageName}", message.Id, message.MessageName);
            message.MarkFailed("Deserialization returned null or unexpected type", dateTimeProvider.UtcNow);
            return;
        }

        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEventType);
        var notification = Activator.CreateInstance(notificationType, domainEvent);

        await mediator.Publish(notification!, cancellationToken);

        message.MarkProcessed(dateTimeProvider.UtcNow);
        _logger.LogDebug("Outbox message {Id}: {MessageName} domain event processed (attempt {Retry})",
            message.Id, message.MessageName, message.RetryCount + 1);
    }

    private async Task DispatchIntegrationEventAsync(
        OutboxMessage message,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventBus integrationEventBus,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var integrationEventType = eventTypeRegistry.GetEventType(message.MessageName);

        if (integrationEventType is null)
        {
            _logger.LogWarning("Outbox message {Id}: integration event type {MessageName} not found in registry", message.Id, message.MessageName);
            message.MarkFailed("EventType not found in registry: " + message.MessageName, dateTimeProvider.UtcNow);
            return;
        }

        var integrationEvent = JsonSerializer.Deserialize(message.PayloadJson, integrationEventType, JsonOptions) as IIntegrationEvent;

        if (integrationEvent is null)
        {
            _logger.LogError("Outbox message {Id}: failed to deserialize integration event payload as {MessageName}", message.Id, message.MessageName);
            message.MarkFailed("Deserialization returned null or unexpected type", dateTimeProvider.UtcNow);
            return;
        }

        await integrationEventBus.PublishAsync(integrationEvent, cancellationToken);

        message.MarkProcessed(dateTimeProvider.UtcNow);
        _logger.LogDebug("Outbox message {Id}: {MessageName} integration event published (attempt {Retry})",
            message.Id, message.MessageName, message.RetryCount + 1);
    }
}
