using Microsoft.EntityFrameworkCore;

namespace Notrelix.Infrastructure.Data.Outbox;

public sealed class ProcessedEventStore : IProcessedEventStore
{
    private readonly ApplicationDbContext _context;

    public ProcessedEventStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsProcessedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProcessedEvent>()
            .AnyAsync(e => e.EventId == eventId && e.ConsumerName == consumerName, cancellationToken);
    }

    public async Task MarkProcessedAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        _context.Set<ProcessedEvent>().Add(processedEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CleanupAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProcessedEvent>()
            .Where(e => e.ProcessedAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
