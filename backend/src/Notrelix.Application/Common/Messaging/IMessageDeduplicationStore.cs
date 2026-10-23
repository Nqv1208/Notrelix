namespace Notrelix.Application.Common.Messaging;

public interface IMessageDeduplicationStore
{
    Task<bool> IsProcessedAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken);

    Task<bool> TryClaimProcessingAsync(
        Guid messageId,
        string consumerName,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically takes over a <c>Processing</c> claim whose last claim/reclaim
    /// timestamp is older than <paramref name="staleBefore"/> (compare-and-swap on
    /// Status == "Processing" AND ClaimedAt &lt; staleBefore), renewing the lease by
    /// setting ClaimedAt to <paramref name="reclaimedAt"/>. Returns true only for the
    /// single caller that won the swap. A <c>Succeeded</c> claim and a claim inside
    /// its lease are never taken over.
    /// </summary>
    Task<bool> TryReclaimStaleProcessingAsync(
        Guid messageId,
        string consumerName,
        DateTimeOffset staleBefore,
        DateTimeOffset reclaimedAt,
        CancellationToken cancellationToken);

    void MarkSucceeded(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt);
}
