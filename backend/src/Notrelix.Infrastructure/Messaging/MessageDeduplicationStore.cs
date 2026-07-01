using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Messaging;

public sealed class MessageDeduplicationStore : IMessageDeduplicationStore
{
    private readonly ApplicationDbContext _context;

    public MessageDeduplicationStore(ApplicationDbContext context)
        => _context = context;

    public async Task<bool> IsProcessedAsync(
        Guid messageId, string consumerName, CancellationToken ct)
        => await _context.Set<MessagingProcessedEvent>()
            .AnyAsync(e => e.EventId == messageId
                && e.ConsumerName == consumerName, ct);

    public void MarkProcessed(
        Guid messageId, string consumerName,
        string messageName, int messageVersion,
        Guid? sourceEventId, Guid? workspaceId,
        DateTimeOffset processedAt)
    {
        _context.Set<MessagingProcessedEvent>().Add(
            new MessagingProcessedEvent(
                eventId: messageId,
                consumerName: consumerName,
                sourceContext: null,
                messageName: messageName,
                messageVersion: messageVersion,
                sourceEventId: sourceEventId,
                subjectType: null,
                subjectId: null,
                workspaceId: workspaceId,
                actorUserId: null,
                correlationId: null,
                causationId: null,
                processedAt: processedAt));
    }
}
