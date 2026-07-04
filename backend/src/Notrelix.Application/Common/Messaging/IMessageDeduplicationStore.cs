namespace Notrelix.Application.Common.Messaging;

public interface IMessageDeduplicationStore
{
    Task<bool> IsProcessedAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken);

    void MarkProcessed(
        Guid messageId,
        string consumerName,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        DateTimeOffset processedAt);
}
