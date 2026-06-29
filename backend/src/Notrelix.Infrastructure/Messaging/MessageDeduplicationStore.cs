using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Messaging;

public sealed class MessageDeduplicationStore : IMessageDeduplicationStore
{
    private readonly ApplicationDbContext _context;

    public MessageDeduplicationStore(ApplicationDbContext context)
        => _context = context;

    public async Task<bool> IsProcessedAsync(
        Guid messageId, string consumerName, CancellationToken ct)
        => await _context.Set<ProcessedEvent>()
            .AnyAsync(e => e.EventId == messageId
                && e.ConsumerName == consumerName, ct);

    public void MarkProcessed(
        Guid messageId, string consumerName,
        string messageName, int messageVersion,
        Guid? sourceEventId, Guid? workspaceId,
        DateTimeOffset processedAt)
    {
        _context.Set<ProcessedEvent>().Add(
            ProcessedEvent.Create(
                messageId, consumerName,
                messageName, messageVersion,
                sourceEventId, workspaceId,
                processedAt));
    }
}
