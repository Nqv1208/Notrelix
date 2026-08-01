using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging;

public sealed class DeduplicationConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IMessageDeduplicationStore _dedupStore;
    private readonly ApplicationDbContext _db;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DeduplicationConsumeFilter<T>> _logger;

    public DeduplicationConsumeFilter(
        IMessageDeduplicationStore dedupStore,
        ApplicationDbContext db,
        IRlsSessionContext rls,
        IDateTimeProvider dateTimeProvider,
        ILogger<DeduplicationConsumeFilter<T>> logger)
    {
        _dedupStore = dedupStore;
        _db = db;
        _rls = rls;
        _dateTimeProvider = dateTimeProvider;
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
            // CRITICAL: Apply RLS trong transaction này cho TẤT CẢ events
            // Bao gồm cả system events (khi _tenant.IsSystemContext == true)
            await _rls.ApplyAsync(context.CancellationToken);

            var claimed = await _dedupStore.TryClaimProcessingAsync(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                messageName: integrationEvent.MessageName,
                messageVersion: integrationEvent.SchemaVersion,
                sourceEventId: integrationEvent.SourceEventId,
                workspaceId: integrationEvent.WorkspaceId,
                cancellationToken: context.CancellationToken);

            if (!claimed)
            {
                _logger.LogDebug(
                    "Event {EventId} ({MessageName}) already claimed/processed by {ConsumerName}, skipping",
                    integrationEvent.EventId, integrationEvent.MessageName, consumerName);
                await transaction.RollbackAsync(context.CancellationToken);
                return;
            }

            await next.Send(context);

            _dedupStore.MarkSucceeded(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                processedAt: _dateTimeProvider.UtcNow);

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
