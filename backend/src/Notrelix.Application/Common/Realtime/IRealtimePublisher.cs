namespace Notrelix.Application.Common.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes facts that differ per recipient (the per-member unread frames of one send). A transport that can do it in one
    /// round trip overrides this; the default publishes them one after the other.
    /// </summary>
    async Task PublishManyAsync(IReadOnlyList<RealtimeResourceChangedV1> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
            await PublishAsync(change, cancellationToken);
    }
}
