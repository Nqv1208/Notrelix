using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
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

            await Task.Delay(OutboxDefaults.PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;

        var messages = await context.Set<OutboxMessage>()
            .FromSqlRaw("""
                SELECT * FROM ops.outbox_messages
                WHERE status = 'Pending' AND next_attempt_at <= {0}
                ORDER BY created_at
                LIMIT {1}
                FOR UPDATE SKIP LOCKED
            """, now.UtcDateTime, OutboxDefaults.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, eventTypeRegistry, mediator, context, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IEventTypeRegistry eventTypeRegistry,
        IMediator mediator,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        message.MarkProcessing();

        try
        {
            var domainEventType = eventTypeRegistry.GetEventType(message.EventType);

            if (domainEventType is null)
            {
                _logger.LogWarning("Outbox message {Id}: event type {EventType} not found", message.Id, message.EventType);
                message.MarkProcessed();
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.PayloadJson, domainEventType, JsonOptions) as IDomainEvent;

            if (domainEvent is null)
            {
                _logger.LogError("Outbox message {Id}: failed to deserialize payload as {EventType}", message.Id, message.EventType);
                message.MarkFailed("Deserialization returned null or unexpected type");
                return;
            }

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEventType);
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            await mediator.Publish(notification, cancellationToken);

            message.MarkProcessed();
            _logger.LogDebug("Outbox message {Id}: {EventType} processed (attempt {Retry})",
                message.Id, message.EventType, message.RetryCount + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox message {Id}: {EventType} failed (attempt {Retry})",
                message.Id, message.EventType, message.RetryCount + 1);
            message.MarkFailed(ex.ToString());
        }
    }
}
