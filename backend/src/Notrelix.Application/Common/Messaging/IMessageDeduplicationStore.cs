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

    void MarkSucceeded(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt);
}
