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
                && e.ConsumerName == consumerName
                && e.Status == "Succeeded", ct);

    public async Task<bool> TryClaimProcessingAsync(
        Guid messageId,
        string consumerName,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        CancellationToken ct)
    {
        try
        {
            var claim = new MessagingProcessedEvent(
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
                claimedAt: DateTimeOffset.UtcNow);

            _context.Set<MessagingProcessedEvent>().Add(claim);
            await _context.SaveChangesAsync(ct);

            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

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
                claimedAt: processedAt));
    }

    public void MarkSucceeded(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt)
    {
        var claim = _context.Set<MessagingProcessedEvent>()
            .FirstOrDefault(e => e.EventId == messageId && e.ConsumerName == consumerName);

        claim?.MarkSucceeded(processedAt);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505");
    }
}
