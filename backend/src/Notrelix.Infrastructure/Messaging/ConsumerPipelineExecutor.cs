using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// Executes integration event consumers with RLS + transaction + idempotency.
/// </summary>
public interface IConsumerPipelineExecutor
{
    Task ExecuteAsync<TEvent>(
        TEvent message,
        string consumerName,
        IIntegrationEventConsumer<TEvent> handler,
        CancellationToken ct)
        where TEvent : IIntegrationEvent;
}

public sealed class ConsumerPipelineExecutor : IConsumerPipelineExecutor
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<ConsumerPipelineExecutor> _logger;

    public ConsumerPipelineExecutor(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        ILogger<ConsumerPipelineExecutor> logger)
    {
        _db = db;
        _tenant = tenant;
        _rls = rls;
        _logger = logger;
    }

    public async Task ExecuteAsync<TEvent>(
        TEvent message,
        string consumerName,
        IIntegrationEventConsumer<TEvent> handler,
        CancellationToken ct)
        where TEvent : IIntegrationEvent
    {
        // Set tenant from message
        if (message.AccountId.HasValue)
        {
            if (message.WorkspaceId.HasValue)
            {
                _tenant.SetWorkspace(
                    message.AccountId.Value,
                    message.WorkspaceId.Value,
                    message.ActorUserId);
            }
            else
            {
                _tenant.SetAccount(message.AccountId.Value, message.ActorUserId);
            }
        }
        else
        {
            _tenant.SetSystem();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Apply RLS with worker scope
            await _rls.ApplyAsync(_db.Database, ct);

            // Idempotency check
            var alreadyProcessed = await _db.MessagingProcessedEvents
                .AnyAsync(e => e.EventId == message.EventId && e.ConsumerName == consumerName, ct);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Event {EventId} already processed by {Consumer}, skipping",
                    message.EventId, consumerName);
                await transaction.RollbackAsync(ct);
                return;
            }

            // Execute handler
            await handler.HandleAsync(message, ct);

            // Mark processed
            _db.MessagingProcessedEvents.Add(new MessagingProcessedEvent(
                eventId: message.EventId,
                consumerName: consumerName,
                sourceContext: null,
                messageName: message.MessageName,
                messageVersion: message.SchemaVersion,
                sourceEventId: message.SourceEventId,
                subjectType: null,
                subjectId: null,
                workspaceId: message.WorkspaceId,
                actorUserId: message.ActorUserId,
                correlationId: message.CorrelationId.ToString(),
                causationId: message.CausationId?.ToString(),
                processedAt: DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogDebug("Event {EventId} processed by {Consumer}",
                message.EventId, consumerName);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            _tenant.Clear();
        }
    }
}
