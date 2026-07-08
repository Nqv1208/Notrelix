using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging;

public sealed class DeduplicationConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IMessageDeduplicationStore _dedupStore;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DeduplicationConsumeFilter<T>> _logger;

    public DeduplicationConsumeFilter(
        IMessageDeduplicationStore dedupStore,
        ApplicationDbContext db,
        ILogger<DeduplicationConsumeFilter<T>> logger)
    {
        _dedupStore = dedupStore;
        _db = db;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message is not IIntegrationEvent integrationEvent)
        {
            await next.Send(context);
            return;
        }

        var consumerName = ExtractConsumerName(context);

        await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            var alreadyProcessed = await _dedupStore.IsProcessedAsync(
                integrationEvent.EventId, consumerName, context.CancellationToken);

            if (alreadyProcessed)
            {
                _logger.LogDebug(
                    "Event {EventId} ({MessageName}) already processed by {ConsumerName}, skipping",
                    integrationEvent.EventId, integrationEvent.MessageName, consumerName);
                await transaction.RollbackAsync(context.CancellationToken);
                return;
            }

            await next.Send(context);

            _dedupStore.MarkProcessed(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                messageName: integrationEvent.MessageName,
                messageVersion: integrationEvent.SchemaVersion,
                sourceEventId: integrationEvent.SourceEventId,
                workspaceId: integrationEvent.WorkspaceId,
                processedAt: DateTimeOffset.UtcNow);

            await _db.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

            _logger.LogDebug(
                "Event {EventId} ({MessageName}) processed by {ConsumerName}",
                integrationEvent.EventId, integrationEvent.MessageName, consumerName);
        }
        catch
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("deduplicationConsumeFilter");
    }

    private static string ExtractConsumerName(ConsumeContext<T> context)
    {
        var inputAddress = context.ReceiveContext.InputAddress;
        if (inputAddress is not null)
        {
            var segments = inputAddress.AbsolutePath.Trim('/').Split('/');
            return segments.Length > 0 ? segments[^1] : $"consumer:{typeof(T).Name}";
        }

        return $"consumer:{typeof(T).Name}";
    }
}
