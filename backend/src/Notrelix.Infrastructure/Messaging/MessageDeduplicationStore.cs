using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.Messaging;

public sealed class MessageDeduplicationStore : IMessageDeduplicationStore, IProviderEffectClaimStore
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MetricsService _metrics;

    public MessageDeduplicationStore(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        MetricsService metrics)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _metrics = metrics;
    }

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
            claimedAt: _dateTimeProvider.UtcNow);

        try
        {
            _context.Set<MessagingProcessedEvent>().Add(claim);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Detach entity để tránh DbContext poisoned sau unique violation
            _context.Entry(claim).State = EntityState.Detached;
            _metrics.InboxDuplicates.Add(1);
            return false;
        }
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

    // ------------------------------------------------------------------
    // IProviderEffectClaimStore — consumer-owned claim lifecycle used by the
    // n8n dispatch endpoint (acquire/prepare tx, out-of-tx effect, settle tx).
    // ------------------------------------------------------------------

    public Task<bool> TryAcquireClaimAsync(
        Guid messageId,
        string consumerName,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        CancellationToken cancellationToken)
        => TryClaimProcessingAsync(
            messageId, consumerName, messageName, messageVersion, sourceEventId, workspaceId, cancellationToken);

    public async Task<MessageClaimInspection> InspectClaimAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        var claim = await _context.Set<MessagingProcessedEvent>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == messageId && e.ConsumerName == consumerName, cancellationToken);

        if (claim is null)
            return new MessageClaimInspection(MessageClaimState.Missing, null);

        var state = claim.Status == "Succeeded"
            ? MessageClaimState.Succeeded
            : MessageClaimState.Processing;
        return new MessageClaimInspection(state, claim.ClaimedAt);
    }

    public async Task<bool> TryReleaseProcessingClaimAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        var deleted = await _context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == messageId
                && e.ConsumerName == consumerName
                && e.Status == "Processing")
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<bool> TryMarkClaimSucceededAsync(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == messageId
                && e.ConsumerName == consumerName
                && e.Status == "Processing")
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.Status, "Succeeded")
                    .SetProperty(e => e.ProcessedAt, processedAt),
                cancellationToken);
        if (updated > 0)
            return true;

        // Provider without ExecuteUpdate (in-memory test stores): fall back to a
        // tracked transition so the caller can still verify affected == 1 before
        // committing its terminal outcome. The caller saves shortly after; the
        // update is scoped to Status == "Processing" here too.
        var claim = await _context.Set<MessagingProcessedEvent>()
            .FirstOrDefaultAsync(
                e => e.EventId == messageId && e.ConsumerName == consumerName, cancellationToken);
        if (claim is not null && claim.Status == "Processing")
        {
            claim.MarkSucceeded(processedAt);
            return true;
        }

        return false;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505");
    }
}
