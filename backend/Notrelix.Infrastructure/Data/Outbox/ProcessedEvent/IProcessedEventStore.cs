namespace Notrelix.Infrastructure.Data.Outbox;

public interface IProcessedEventStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
    Task<int> CleanupAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
}
